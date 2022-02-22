using System.ComponentModel.DataAnnotations;
using System.Net;
using CodeRower.CCP.Controllers.Models;
using CodeRower.CCP.Controllers.Models.Common;
using CodeRower.CCP.Controllers.Models.Enums;
using CodeRower.CCP.Controllers.Models.Transfers;
using CodeRower.CCP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CodeRower.CCP.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/tenant/{tenantId}/transfers")]
    public class TransfersController : Controller
    {
        private readonly ITransactionsService _transactionsService;
        private readonly ITenantService _tenantService;
        private readonly IUsersService _usersService;
        private readonly IConfiguration _configuration;
        private readonly ICustomerService _customerService;

        public TransfersController(ITransactionsService transactionsService, IUsersService usersService, ITenantService tenantService,
            IConfiguration configuration, ICustomerService customerService)
        {
            _transactionsService = transactionsService;
            _usersService = usersService;
            _tenantService = tenantService;
            _configuration = configuration;
            _customerService = customerService;
        }

        [HttpPost("unlocked-to-wallet")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferUnlockedCoinsAsync([FromRoute, Required] Guid tenantId, [FromBody, Required] UnlockedTransferRequest TransferRequest)
        {
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            List<WalletTransactionResponse> transactions = new List<WalletTransactionResponse>();

            var unlockedBalance = (await _transactionsService
                                .GetBalancesByTransactionTypes(new List<string> { "UNLOCKED" }, customerId)
                                .ConfigureAwait(false))?.FirstOrDefault()
                                ?.Amount ?? 0;

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);
            var unlockToWalletFeePct = tenantInfo?.UnlockToWalletFeePct ?? 0;
            var unlockToWalletFeeAmount = TransferRequest.Amount * unlockToWalletFeePct / 100;
            var amountTobeDeducted = TransferRequest.Amount + unlockToWalletFeeAmount;

            if (unlockedBalance < amountTobeDeducted)
            {
                ModelState.AddModelError(nameof(MintRequest.Amount), "Insufficient funds.");
                return BadRequest(ModelState);
            }

            // Debit from account
            var debitTran = await _transactionsService.AddTransaction(new TransactionRequest
            {
                Amount = TransferRequest.Amount,
                IsCredit = false,
                Reference = $"Transfer to payee {customerId}",
                PayerId = customerId,
                PayeeId = customerId,
                TransactionType = "UNLOCKED",
                Currency = Currency.COINS
            }).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(debitTran?.transactionid))
            {
                transactions.Add(debitTran);

                // Debit from account
                var debitFeeTran = await _transactionsService.AddTransaction(new TransactionRequest
                {
                    Amount = unlockToWalletFeeAmount,
                    IsCredit = false,
                    Reference = $"Fee deducted for transfer unlocked coins to Wallet to payee {customerId}",
                    PayerId = customerId,
                    PayeeId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value,
                    TransactionType = "UNLOCKED",
                    Currency = Currency.COINS,
                    BaseTransaction = debitTran?.transactionid
                }).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(debitFeeTran?.transactionid))
                {
                    transactions.Add(debitFeeTran);

                    // Credit fee
                    var creditFeeTran = await _transactionsService.AddTransaction(new TransactionRequest
                    {
                        Amount = unlockToWalletFeeAmount,
                        IsCredit = true,
                        Reference = $"Fee deducted for transfer unlocked coins to Wallet to payee {customerId}",
                        PayerId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value,
                        PayeeId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value,
                        TransactionType = "UNLOCKED_WALLET_FEE",
                        Currency = Currency.COINS,
                        BaseTransaction = debitTran?.transactionid
                    }).ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(creditFeeTran?.transactionid))
                    {
                        transactions.Add(creditFeeTran);

                        // Credit to other account
                        var creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                        {
                            Amount = TransferRequest.Amount,
                            IsCredit = true,
                            Reference = "Transferred from UNLOCKED coins to WALLET",
                            PayerId = customerId,
                            PayeeId = customerId,
                            TransactionType = "WALLET",
                            Currency = Currency.COINS,
                            BaseTransaction = debitTran?.transactionid
                        }).ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(creditTran?.transactionid))
                        {
                            transactions.Add(creditTran);
                        }

                    }
                }
            }

            return transactions.Any() ? Ok(new ListResponse<WalletTransactionResponse> { Rows = transactions }) : NoContent();
        }

        [HttpPost("wallet-to-wallet")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferWalletCoinsAsync([FromRoute, Required] Guid tenantId, [FromBody, Required] CoinsTransferRequest TransferRequest)
        {
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;

            var customerInfo = await _customerService.GetCustomerInfoAsync(null, TransferRequest.ToCustomerId).ConfigureAwait(false);

            if (customerInfo == null)
            {
                ModelState.AddModelError(nameof(TransferRequest.ToCustomerId), "Wallet Address not found.");
                return BadRequest(ModelState);
            }
            var toCustomerId = customerInfo.Id;
            List<WalletTransactionResponse> transactions = new List<WalletTransactionResponse>();

            var walletBalance = (await _transactionsService
                                .GetBalancesByTransactionTypes(new List<string> { "WALLET" }, customerId)
                                .ConfigureAwait(false))?.FirstOrDefault()
                                ?.Amount ?? 0;

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);
            var walletTransferFeePct = tenantInfo?.WalletToWalletFeePct ?? 0;
            var minWithDrawalLimit = tenantInfo?.MinWithdrawalLimitInUSD ?? 0;
            var walletTransferFeeAmount = TransferRequest.Amount * walletTransferFeePct / 100;
            var amountTobeDeducted = TransferRequest.Amount + walletTransferFeeAmount;

            if (TransferRequest.Amount <= minWithDrawalLimit)
            {
                ModelState.AddModelError(nameof(TransferRequest.Amount), "Transfer amount should be greater than minimum transfer amount.");
            }

            if (amountTobeDeducted > walletBalance)
            {
                ModelState.AddModelError(nameof(TransferRequest.Amount), "Insufficient funds.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Debit from account
            var debitTran = await _transactionsService.AddTransaction(new TransactionRequest
            {
                Amount = TransferRequest.Amount,
                IsCredit = false,
                Reference = $"Transfer to payee {TransferRequest.ToCustomerId}",
                PayerId = customerId,
                PayeeId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value,
                TransactionType = "WALLET",
                Currency = Currency.COINS
            }).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(debitTran?.transactionid))
            {
                transactions.Add(debitTran);

                // Debit fee from account
                var debitFeeTran = await _transactionsService.AddTransaction(new TransactionRequest
                {
                    Amount = walletTransferFeeAmount,
                    IsCredit = false,
                    Reference = $"Fee deducted for wallet to wallet Transfer to payee {TransferRequest.ToCustomerId}",
                    PayerId = customerId,
                    PayeeId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value,
                    TransactionType = "WALLET",
                    Currency = Currency.COINS,
                    BaseTransaction = debitTran?.transactionid
                }).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(debitFeeTran?.transactionid))
                {
                    transactions.Add(debitFeeTran);

                    // Credit fee to tenant
                    var creditFeeTran = await _transactionsService.AddTransaction(new TransactionRequest
                    {
                        Amount = walletTransferFeeAmount,
                        IsCredit = true,
                        Reference = $"Fee deducted for wallet to wallet Transfer to payee {TransferRequest.ToCustomerId}",
                        PayerId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value,
                        PayeeId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value,
                        TransactionType = "WALLET_WALLET_FEE",
                        Currency = Currency.COINS,
                        BaseTransaction = debitTran?.transactionid
                    }).ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(creditFeeTran?.transactionid))
                    {
                        transactions.Add(creditFeeTran);
                        // Credit to other account
                        var creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                        {
                            Amount = TransferRequest.Amount,
                            IsCredit = true,
                            Reference = $"Received from payer {customerId}",
                            PayerId = _configuration.GetSection("AppSettings:CCCWalletTenant").Value,
                            PayeeId = toCustomerId,
                            TransactionType = "WALLET",
                            Remark = $"Wallet transfer received from {customerId}",
                            Currency = Currency.COINS,
                            BaseTransaction = debitTran?.transactionid
                        }).ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(creditTran?.transactionid))
                        {
                            transactions.Add(creditTran);
                        }
                    }
                }
            }

            return transactions.Any() ? Ok(new ListResponse<WalletTransactionResponse> { Rows = transactions }) : NoContent();
        }

        [HttpPost("locked-to-mint")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferToMintAsync([FromRoute, Required] Guid tenantId,[FromBody, Required] MintRequest MintRequest)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            var userInfo = await _usersService.GetUserInfoAsync(userId).ConfigureAwait(false);

            if (MintRequest.AccountPin != userInfo?.AccountPin)
            {
                ModelState.AddModelError(nameof(MintRequest.AccountPin), "Invalid account pin.");
            }

            var lockedBalance = (await _transactionsService
                                .GetBalancesByTransactionTypes(new List<string> { "LOCKED" }, customerId)
                                .ConfigureAwait(false))?.FirstOrDefault()
                                ?.Amount ?? 0;

            if (MintRequest.Amount > lockedBalance)
            {
                ModelState.AddModelError(nameof(MintRequest.Amount), "Insufficient funds.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            List<WalletTransactionResponse> transactions = new List<WalletTransactionResponse>();

            // Debit locked
            var debitTran = await _transactionsService.AddTransaction(new TransactionRequest
            {
                Amount = MintRequest.Amount,
                IsCredit = false,
                Reference = $"Transfer to payee {customerId}",
                PayerId = customerId,
                PayeeId = customerId,
                TransactionType = "LOCKED",
                Currency = Currency.COINS
            }).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(debitTran?.transactionid))
            {
                transactions.Add(debitTran);

                // Credit to mint
                var creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                {
                    Amount = MintRequest.Amount,
                    IsCredit = true,
                    Reference = debitTran.transactionid,
                    PayerId = customerId,
                    PayeeId = customerId,
                    TransactionType = "MINT",
                    Currency = Currency.COINS
                }).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(creditTran?.transactionid))
                {
                    transactions.Add(creditTran);
                }
            }

            return transactions.Any() ? Ok(new ListResponse<WalletTransactionResponse> { Rows = transactions }) : NoContent();
        }

        [HttpPost("locked-to-farm")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferToFarmAsync([FromRoute, Required] Guid tenantId, [FromBody, Required] MintRequest FarmRequest)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            var userInfo = await _usersService.GetUserInfoAsync(userId).ConfigureAwait(false);

            if (FarmRequest.AccountPin != userInfo?.AccountPin)
            {
                ModelState.AddModelError(nameof(MintRequest.AccountPin), "Invalid account pin.");
            }

            var lockedBalance = (await _transactionsService
                                .GetBalancesByTransactionTypes(new List<string> { "LOCKED" }, customerId)
                                .ConfigureAwait(false))?.FirstOrDefault()
                                ?.Amount ?? 0;

            if (FarmRequest.Amount > lockedBalance)
            {
                ModelState.AddModelError(nameof(MintRequest.Amount), "Insufficient funds.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            List<WalletTransactionResponse> transactions = new List<WalletTransactionResponse>();

            // Debit locked
            var debitTran = await _transactionsService.AddTransaction(new TransactionRequest
            {
                Amount = FarmRequest.Amount,
                IsCredit = false,
                Reference = $"Transfer to payee {customerId}",
                PayerId = customerId,
                PayeeId = customerId,
                TransactionType = "LOCKED",
                Currency = Currency.COINS
            }).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(debitTran?.transactionid))
            {
                transactions.Add(debitTran);

                // Credit to mint
                var creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                {
                    Amount = FarmRequest.Amount,
                    IsCredit = true,
                    Reference = debitTran.transactionid,
                    PayerId = customerId,
                    PayeeId = customerId,
                    TransactionType = "FARM",
                    Currency = Currency.COINS
                }).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(creditTran?.transactionid))
                {
                    transactions.Add(creditTran);
                }
            }

            return transactions.Any() ? Ok(new ListResponse<WalletTransactionResponse> { Rows = transactions }) : NoContent();
        }
    }
}