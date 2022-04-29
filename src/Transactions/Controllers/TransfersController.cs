using System.ComponentModel.DataAnnotations;
using System.Net;
using CodeRower.CCP.Controllers.Domain;
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
    [Route("api/tenant/{tenantId}/transfers")]
    public class TransfersController : Controller
    {
        private readonly ITransactionsService _transactionsService;
        private readonly ITenantService _tenantService;
        private readonly ICustomerService _customerService;
        private readonly ISmsService _smsService;

        public TransfersController(ITransactionsService transactionsService, IUsersService usersService,
            ITenantService tenantService, ICustomerService customerService, ISmsService smsService)
        {
            _transactionsService = transactionsService;
            _tenantService = tenantService;
            _customerService = customerService;
            _smsService = smsService;
        }

        [Authorize]
        [HttpPost("unlocked-to-wallet")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferUnlockedCoinsAsync([FromRoute, Required] Guid tenantId,
            [FromBody, Required] UnlockedTransferRequest TransferRequest)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            List<WalletTransactionResponse> transactions = new List<WalletTransactionResponse>();

            var unlockedBalance = (await _transactionsService
                                .GetBalancesByTransactionTypes(tenantId, new List<string> { "UNLOCKED" }, customerId)
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

            if (!(await _smsService.VerifyAsync(tenantId, new Guid(userId), TransferRequest.Otp, "unlocked-to-wallet").ConfigureAwait(false)))
            {
                ModelState.AddModelError(nameof(MintRequest.Otp), "Invalid Otp.");
                return BadRequest(ModelState);
            }

            // Debit from account
            var debitTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
            {
                Amount = TransferRequest.Amount,
                IsCredit = false,
                Reference = $"Transfer to payee {customerId}",
                PayerId = customerId,
                PayeeId = tenantInfo.WalletTenantId,
                TransactionType = "UNLOCKED",
                Currency = Currency.COINS
            }).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(debitTran?.transactionid))
            {
                transactions.Add(debitTran);

                // Debit from account
                var debitFeeTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = unlockToWalletFeeAmount,
                    IsCredit = false,
                    Reference = $"Fee deducted for transfer unlocked coins to Wallet to payee {customerId}",
                    PayerId = customerId,
                    PayeeId = tenantInfo.WalletTenantId,
                    TransactionType = "UNLOCKED_WALLET_FEE",
                    Currency = Currency.COINS,
                    BaseTransaction = debitTran?.transactionid
                }).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(debitFeeTran?.transactionid))
                {
                    transactions.Add(debitFeeTran);

                    // Credit to other account
                    var creditTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                    {
                        Amount = TransferRequest.Amount,
                        IsCredit = true,
                        Reference = "Transferred from UNLOCKED coins to WALLET",
                        PayerId = tenantInfo.WalletTenantId,
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

            return transactions.Any() ? Ok(new ListResponse<WalletTransactionResponse> { Rows = transactions }) : NoContent();
        }

        [Authorize]
        [HttpPost("wallet-to-wallet")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferWalletCoinsAsync([FromRoute, Required] Guid tenantId,
            [FromBody, Required] CoinsTransferRequest TransferRequest)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;

            var customerInfo = await _customerService.GetCustomerInfoAsync(tenantId, null, TransferRequest.ToCustomerId).ConfigureAwait(false);

            if (customerInfo == null)
            {
                ModelState.AddModelError(nameof(TransferRequest.ToCustomerId), "Wallet Address not found.");
                return BadRequest(ModelState);
            }

            var toCustomerId = customerInfo.Id;
            List<WalletTransactionResponse> transactions = new List<WalletTransactionResponse>();

            var walletBalance = (await _transactionsService
                                .GetBalancesByTransactionTypes(tenantId, new List<string> { "WALLET" }, customerId)
                                .ConfigureAwait(false))?.FirstOrDefault()
                                ?.Amount ?? 0;

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);
            var walletTransferFeePct = tenantInfo?.WalletToWalletFeePct ?? 0;
            var minWithDrawalLimit = 1m;
            var walletTransferFeeAmount = TransferRequest.Amount * walletTransferFeePct / 100;
            var amountTobeDeducted = TransferRequest.Amount + walletTransferFeeAmount;
            var latestRate = tenantInfo.LatestRateInUSD;

            if (TransferRequest.Amount * latestRate < minWithDrawalLimit)
            {
                ModelState.AddModelError(nameof(TransferRequest.Amount), $"Transfer amount should be greater than minimum transfer amount - ${minWithDrawalLimit}.");
                return BadRequest(ModelState);
            }

            if (amountTobeDeducted > walletBalance)
            {
                ModelState.AddModelError(nameof(TransferRequest.Amount), "Insufficient funds.");
                return BadRequest(ModelState);
            }

            if (!(await _smsService.VerifyAsync(tenantId, new Guid(userId), TransferRequest.Otp, "wallet-to-wallet").ConfigureAwait(false)))
            {
                ModelState.AddModelError(nameof(MintRequest.Otp), "Invalid Otp.");
                return BadRequest(ModelState);
            }

            // Debit from account
            var debitTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
            {
                Amount = TransferRequest.Amount,
                IsCredit = false,
                Reference = $"Transfer to payee {TransferRequest.ToCustomerId}",
                PayerId = customerId,
                PayeeId = tenantInfo.WalletTenantId,
                TransactionType = "WALLET",
                Currency = Currency.COINS
            }).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(debitTran?.transactionid))
            {
                transactions.Add(debitTran);

                // Debit fee from account
                var debitFeeTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = walletTransferFeeAmount,
                    IsCredit = false,
                    Reference = $"Fee deducted for wallet to wallet Transfer to payee {TransferRequest.ToCustomerId}",
                    PayerId = customerId,
                    PayeeId = tenantInfo.WalletTenantId,
                    TransactionType = "WALLET_WALLET_FEE",
                    Currency = Currency.COINS,
                    BaseTransaction = debitTran?.transactionid
                }).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(debitFeeTran?.transactionid))
                {
                    transactions.Add(debitFeeTran);

                    // Credit to other account
                    var creditTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                    {
                        Amount = TransferRequest.Amount,
                        IsCredit = true,
                        Reference = $"Received from payer {customerId}",
                        PayerId = tenantInfo.WalletTenantId,
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

            return transactions.Any() ? Ok(new ListResponse<WalletTransactionResponse> { Rows = transactions }) : NoContent();
        }

        [Authorize]
        [HttpPost("locked-to-mint")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferToMintAsync([FromRoute, Required] Guid tenantId,
            [FromBody, Required] MintRequest MintRequest)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;

            var lockedBalance = (await _transactionsService
                                .GetBalancesByTransactionTypes(tenantId, new List<string> { "LOCKED" }, customerId)
                                .ConfigureAwait(false))?.FirstOrDefault()
                                ?.Amount ?? 0;

            if (MintRequest.Amount > lockedBalance)
            {
                ModelState.AddModelError(nameof(MintRequest.Amount), "Insufficient funds.");
                return BadRequest(ModelState);
            }

            if (!(await _smsService.VerifyAsync(tenantId, new Guid(userId), MintRequest.Otp, "locked-to-mint").ConfigureAwait(false)))
            {
                ModelState.AddModelError(nameof(MintRequest.Otp), "Invalid Otp.");

                return BadRequest(ModelState);
            }

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);
            List<WalletTransactionResponse> transactions = new List<WalletTransactionResponse>();

            // Debit locked
            var debitTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
            {
                Amount = MintRequest.Amount,
                IsCredit = false,
                Reference = $"Transfer to payee {customerId}",
                PayerId = customerId,
                PayeeId = tenantInfo.WalletTenantId,
                TransactionType = "LOCKED",
                Currency = Currency.COINS
            }).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(debitTran?.transactionid))
            {
                transactions.Add(debitTran);

                // Credit to mint
                var creditTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = MintRequest.Amount,
                    IsCredit = true,
                    Reference = "Transfer successfully to MINT",
                    PayerId = tenantInfo.WalletTenantId,
                    PayeeId = customerId,
                    TransactionType = "MINT",
                    Currency = Currency.COINS,
                    BaseTransaction = debitTran.transactionid
                }).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(creditTran?.transactionid))
                {
                    transactions.Add(creditTran);
                }
            }

            return transactions.Any() ? Ok(new ListResponse<WalletTransactionResponse> { Rows = transactions }) : NoContent();
        }

        [Authorize]
        [HttpPost("locked-to-farm")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferToFarmAsync([FromRoute, Required] Guid tenantId,
            [FromBody, Required] MintRequest FarmRequest)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;

            var lockedBalance = (await _transactionsService
                                .GetBalancesByTransactionTypes(tenantId, new List<string> { "LOCKED" }, customerId)
                                .ConfigureAwait(false))?.FirstOrDefault()
                                ?.Amount ?? 0;

            if (FarmRequest.Amount > lockedBalance)
            {
                ModelState.AddModelError(nameof(MintRequest.Amount), "Insufficient funds.");
                return BadRequest(ModelState);
            }

            if (!(await _smsService.VerifyAsync(tenantId, new Guid(userId), FarmRequest.Otp, "locked-to-farm").ConfigureAwait(false)))
            {
                ModelState.AddModelError(nameof(MintRequest.Otp), "Invalid Otp.");
                return BadRequest(ModelState);
            }

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);
            List<WalletTransactionResponse> transactions = new List<WalletTransactionResponse>();

            // Debit locked
            var debitTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
            {
                Amount = FarmRequest.Amount,
                IsCredit = false,
                Reference = $"Transfer to payee {customerId}",
                PayerId = customerId,
                PayeeId = tenantInfo.WalletTenantId,
                TransactionType = "LOCKED",
                Currency = Currency.COINS
            }).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(debitTran?.transactionid))
            {
                transactions.Add(debitTran);

                // Credit to mint
                var creditTran = await _transactionsService.AddTransaction(tenantId, new TransactionRequest
                {
                    Amount = FarmRequest.Amount,
                    IsCredit = true,
                    Reference = "Transfer successfull to FARM",
                    PayerId = tenantInfo.WalletTenantId,
                    PayeeId = customerId,
                    TransactionType = "FARM",
                    Currency = Currency.COINS,
                    BaseTransaction = debitTran.transactionid
                }).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(creditTran?.transactionid))
                {
                    transactions.Add(creditTran);
                }
            }

            return transactions.Any() ? Ok(new ListResponse<WalletTransactionResponse> { Rows = transactions }) : NoContent();
        }

        [Authorize]
        [HttpPost("wallet-to-cpwallet")]
        [SwaggerResponse((int)HttpStatusCode.OK)]
        public async Task<IActionResult> TransferWalletCoinsToCPAsync([FromRoute, Required] Guid tenantId,
            [FromBody, Required] CoinsTransferToCPRequest TransferRequest)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;

            List<WalletTransactionResponse> transactions = new List<WalletTransactionResponse>();

            var transactionsBalance = await _transactionsService
                                .GetBalancesByTransactionTypes(tenantId, new List<string> { "WALLET" }, userId: userId)
                                .ConfigureAwait(false);

            var effectiveBalance = transactionsBalance.Where(item => item.TransactionType == "EFFECTIVE_BALANCE")?
                                    .FirstOrDefault()?.Amount ?? 0;

            var tenantInfo = await _tenantService.GetTenantInfo(tenantId).ConfigureAwait(false);
            var bankTransferFeePct = tenantInfo?.BankAccountWithdrawalFeePct ?? 0;
            var minWithDrawalLimit = tenantInfo?.MinWithdrawalLimitInUSD ?? 0;
            var walletTransferFeeAmount = TransferRequest.Amount * bankTransferFeePct / 100;
            var amountTobeDeducted = TransferRequest.Amount + walletTransferFeeAmount;
            var latestRate = tenantInfo.LatestRateInUSD;

            if (TransferRequest.Amount * latestRate < minWithDrawalLimit)
            {
                ModelState.AddModelError(nameof(TransferRequest.Amount), $"Transfer amount should be greater than minimum transfer amount - ${minWithDrawalLimit}.");
                return BadRequest(ModelState);
            }

            if (amountTobeDeducted > effectiveBalance * latestRate)
            {
                ModelState.AddModelError(nameof(TransferRequest.Amount), "Insufficient funds.");
                return BadRequest(ModelState);
            }

            if (!(await _smsService.VerifyAsync(tenantId, new Guid(userId), TransferRequest.Otp, "wallet-to-cpwallet").ConfigureAwait(false)))
            {
                ModelState.AddModelError(nameof(MintRequest.Otp), "Invalid Otp.");
                return BadRequest(ModelState);
            }

            CoinsTransferToCPRequestDTO request = new CoinsTransferToCPRequestDTO()
            {
                Amount = TransferRequest.Amount,
                FeeAmount = walletTransferFeeAmount,
                Message = TransferRequest.Message,
                AmountInCC = TransferRequest.Amount / latestRate,
                FeeAmountInCC = walletTransferFeeAmount / latestRate,
                Currency = tenantInfo.LicenseCostCurrency,
                TransactionType = "CP_WITHDRAWAL"
            };

            var bearerToken = Request.Headers.Authorization.FirstOrDefault()?.ToString();

            var transaction = await _transactionsService.AddToTransactionBooks(tenantId, new Guid(userId), request, bearerToken).ConfigureAwait(false);
            return transaction != null ? Ok(transaction.transactionid) : NoContent();
        }

        [HttpPost("wallet-to-cpwallet/settle")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(WalletTransactionResponse))]
        public async Task<IActionResult> TransferWalletCoinsToCPAsync([FromRoute, Required] Guid tenantId,
            [FromBody, Required] CoinsTransferToCPSettleRequestData Data)
        {
            if (Data.Data.AuthKey == "b0126d73-c22a-4275-b4b6-bfca60ac3eaf")
            {
                var transaction = await _transactionsService.SettleWalletToCpWalletTransaction(tenantId, new Guid(Data.Data.TransactionId)).ConfigureAwait(false);

                return Ok(transaction);
            }

            ModelState.AddModelError(nameof(Data.Data.AuthKey), "Auth Key is required.");

            return BadRequest(ModelState);
        }

    }
}