using AutoMapper;
using TransactionManager.Data.Models;
using TransactionManager.Dtos;
using TransactionManager.Views;

namespace TransactionManager.Mapper;

public class TransactionManagerProfile : Profile
{
    public TransactionManagerProfile()
    {
        CreateMap<DebitTransaction, TransactionDto>()
            .ForMember(dst => dst.TransactionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dst => dst.DateTime, opt => opt.MapFrom(src => src.DateTime))
            .ForMember(dst => dst.ClientId, opt => opt.MapFrom(src => src.ClientId))
            .ForMember(dst => dst.Debit, opt => opt.MapFrom(src => src.Amount));

        CreateMap<CreditTransaction, TransactionDto>()
            .ForMember(dst => dst.TransactionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dst => dst.DateTime, opt => opt.MapFrom(src => src.DateTime))
            .ForMember(dst => dst.ClientId, opt => opt.MapFrom(src => src.ClientId))
            .ForMember(dst => dst.Credit, opt => opt.MapFrom(src => src.Amount));

        CreateMap<TransactionDto, TransactionModel>()
            .ForMember(dst => dst.TransactionId, opt => opt.MapFrom(src => src.TransactionId))
            .ForMember(dst => dst.Date, opt => opt.MapFrom(src => src.DateTime))
            .ForMember(dst => dst.ClientId, opt => opt.MapFrom(src => src.ClientId))
            .ForMember(dst => dst.Debit, opt => opt.MapFrom(src => src.Debit))
            .ForMember(dst => dst.Credit, opt => opt.MapFrom(src => src.Credit));

        CreateMap<ClientBalanceDto, ClientBalanceResult>()
            .ForMember(dst => dst.ClientBalance, opt => opt.MapFrom(src => src.Balance))
            .ForMember(dst => dst.BalanceDateTime, opt => opt.MapFrom(src => src.BalanceDateTime));
    }
}