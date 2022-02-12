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
    [Route("api/transfers")]
    public class TransfersController : Controller
    {
        private readonly ITransactionsService _transactionsService;
        private readonly IUsersService _usersService;

        public TransfersController(ITransactionsService transactionsService, IUsersService usersService)
        {
            _transactionsService = transactionsService;
            _usersService = usersService;
        }

        [HttpPost("unlocked-to-wallet")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferUnlockedCoinsAsync([FromBody, Required] UnlockedTransferRequest TransferRequest)
        { 
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            List<Transaction> transactions = new List<Transaction>();

            var unlockedBalance = (await _transactionsService
                                .GetBalancesByTransactionTypes(new List<string> { "UNLOCKED" }, customerId)
                                .ConfigureAwait(false))?.FirstOrDefault()
                                ?.Amount ?? 0;

            if(TransferRequest.Amount > unlockedBalance)
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

            if(debitTran?.Id.HasValue ?? false)
            {
                transactions.Add(debitTran);

                // Credit to other account
                var creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                {
                    Amount = TransferRequest.Amount,
                    IsCredit = true,
                    Reference = debitTran.Id.Value.ToString(),
                    PayerId = customerId,
                    PayeeId = customerId,
                    TransactionType = "WALLET",
                    Currency = Currency.COINS
                }).ConfigureAwait(false);

                if(creditTran?.Id.HasValue ?? false)
                {
                    transactions.Add(creditTran);
                }
            }

            return transactions.Any() ? Ok(new ListResponse<Transaction> { Rows = transactions }) : NoContent();
        }

        [HttpPost("wallet-to-wallet")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferWalletCoinsAsync([FromBody, Required] CoinsTransferRequest TransferRequest)
        {
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            List<Transaction> transactions = new List<Transaction>();

            var unlockedBalance = (await _transactionsService
                                .GetBalancesByTransactionTypes(new List<string> { "WALLET" }, customerId)
                                .ConfigureAwait(false))?.FirstOrDefault()
                                ?.Amount ?? 0;

            if(TransferRequest.Amount > unlockedBalance)
            {
                ModelState.AddModelError(nameof(MintRequest.Amount), "Insufficient funds.");
                return BadRequest(ModelState);
            }

            // Debit from account
            var debitTran = await _transactionsService.AddTransaction(new TransactionRequest
            {
                Amount = TransferRequest.Amount,
                IsCredit = false,
                Reference = $"Transfer to payee {TransferRequest.ToCustomerId}",
                PayerId = customerId,
                PayeeId = TransferRequest.ToCustomerId,
                TransactionType = "WALLET",
                Currency = Currency.COINS
            }).ConfigureAwait(false);

            if(debitTran?.Id.HasValue ?? false)
            {
                transactions.Add(debitTran);

                // Credit to other account
                var creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                {
                    Amount = TransferRequest.Amount,
                    IsCredit = true,
                    Reference = debitTran.Id.Value.ToString(),
                    PayerId = customerId,
                    PayeeId = TransferRequest.ToCustomerId,
                    TransactionType = "WALLET",
                    Currency = Currency.COINS
                }).ConfigureAwait(false);

                if(creditTran?.Id.HasValue ?? false)
                {
                    transactions.Add(creditTran);
                }
            }

            return transactions.Any() ? Ok(new ListResponse<Transaction> { Rows = transactions }) : NoContent();
        }

        [HttpPost("locked-to-mint")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferToMintAsync([FromBody, Required] MintRequest MintRequest)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            var userInfo = await _usersService.GetUserInfoAsync(userId).ConfigureAwait(false);

            if(MintRequest.AccountPin != userInfo?.AccountPin)
            {
                ModelState.AddModelError(nameof(MintRequest.AccountPin), "Invalid account pin.");
            }

            var lockedBalance = (await _transactionsService
                                .GetBalancesByTransactionTypes(new List<string> { "LOCKED" }, customerId)
                                .ConfigureAwait(false))?.FirstOrDefault()
                                ?.Amount ?? 0;

            if(MintRequest.Amount > lockedBalance)
            {
                ModelState.AddModelError(nameof(MintRequest.Amount), "Insufficient funds.");
            }

            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            List<Transaction> transactions = new List<Transaction>();

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

            if(debitTran?.Id.HasValue ?? false)
            {
                transactions.Add(debitTran);

                // Credit to mint
                var creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                {
                    Amount = MintRequest.Amount,
                    IsCredit = true,
                    Reference = debitTran.Id.Value.ToString(),
                    PayerId = customerId,
                    PayeeId = customerId,
                    TransactionType = "MINT",
                    Currency = Currency.COINS
                }).ConfigureAwait(false);

                if(creditTran?.Id.HasValue ?? false)
                {
                    transactions.Add(creditTran);
                }
            }

            return transactions.Any() ? Ok(new ListResponse<Transaction> { Rows = transactions }) : NoContent();
        }

        [HttpPost("locked-to-farm")]
        [SwaggerResponse((int)HttpStatusCode.OK, Type = typeof(ListResponse<Transaction>))]
        public async Task<IActionResult> TransferToFarmAsync([FromBody, Required] MintRequest FarmRequest)
        {
            var userId = User?.Claims?.FirstOrDefault(c => c.Type == "id")?.Value;
            var customerId = User?.Claims?.FirstOrDefault(c => c.Type == "customerId")?.Value;
            var userInfo = await _usersService.GetUserInfoAsync(userId).ConfigureAwait(false);

            if(FarmRequest.AccountPin != userInfo?.AccountPin)
            {
                ModelState.AddModelError(nameof(MintRequest.AccountPin), "Invalid account pin.");
            }

            var lockedBalance = (await _transactionsService
                                .GetBalancesByTransactionTypes(new List<string> { "LOCKED" }, customerId)
                                .ConfigureAwait(false))?.FirstOrDefault()
                                ?.Amount ?? 0;

            if(FarmRequest.Amount > lockedBalance)
            {
                ModelState.AddModelError(nameof(MintRequest.Amount), "Insufficient funds.");
            }

            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            List<Transaction> transactions = new List<Transaction>();

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

            if(debitTran?.Id.HasValue ?? false)
            {
                transactions.Add(debitTran);

                // Credit to mint
                var creditTran = await _transactionsService.AddTransaction(new TransactionRequest
                {
                    Amount = FarmRequest.Amount,
                    IsCredit = true,
                    Reference = debitTran.Id.Value.ToString(),
                    PayerId = customerId,
                    PayeeId = customerId,
                    TransactionType = "FARM",
                    Currency = Currency.COINS
                }).ConfigureAwait(false);

                if(creditTran?.Id.HasValue ?? false)
                {
                    transactions.Add(creditTran);
                }
            }

            return transactions.Any() ? Ok(new ListResponse<Transaction> { Rows = transactions }) : NoContent();
        }
    }
}