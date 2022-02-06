using AutoMapper;
using CodeRower.CCP.Controllers.Models;
using DomainTransaction = Transactions.Domain.Models.Transaction;
using DomainTransactionType = Transactions.Domain.Models.TransactionType;

namespace Transactions
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {

            CreateMap<DomainTransactionType, TransactionType>();
            CreateMap<DomainTransaction, Transaction>();
        }
    }
}