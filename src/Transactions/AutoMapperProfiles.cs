using AutoMapper;
using Transactions.Controllers.Models;
using Transactions.Controllers.Models.Enums;
using DomainTransaction = Transactions.Domain.Models.Transaction;
using DomainTransactionType = Transactions.Domain.Models.TransactionType;

namespace Transactions
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {

            CreateMap<DomainTransactionType, TransactionType>();
            CreateMap<DomainTransaction, Transaction>()
                .AfterMap((src, dest) => 
                {
                    dest.Currency = Enum.Parse<Currency>(src.CurrencyISO ?? string.Empty);
                    dest.Amount = src.Amounts == null ? (decimal?)null : src.Amounts.COINS;
                });
        }
    }
}