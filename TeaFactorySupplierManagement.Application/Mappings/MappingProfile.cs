using AutoMapper;
using TeaFactorySupplierManagement.Application.DTOs.Auth;
using TeaFactorySupplierManagement.Application.DTOs.Factory;
using TeaFactorySupplierManagement.Application.DTOs.Leaf;
using TeaFactorySupplierManagement.Application.DTOs.Notifications;
using TeaFactorySupplierManagement.Application.DTOs.Requests;
using TeaFactorySupplierManagement.Application.DTOs.Settings;
using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Auth mappings
            CreateMap<RegSupplier, RegistrationDto>()
                .ForMember(dest => dest.AccountNumber, opt => opt.MapFrom(src => src.AccCode));

            // Leaf mappings
            CreateMap<TrLeafCollectionTemp, LeafCollectionDto>()
                .ForMember(dest => dest.Day, opt => opt.MapFrom(src => src.DayNo ?? 0))
                .ForMember(dest => dest.Gross, opt => opt.MapFrom(src => src.Gross ?? 0))
                .ForMember(dest => dest.Bags, opt => opt.MapFrom(src => 1))
                .ForMember(dest => dest.Water, opt => opt.MapFrom(src => src.Water ?? 0))
                .ForMember(dest => dest.NetWeight, opt => opt.MapFrom(src => src.NetWeight ?? 0))
                .ForMember(dest => dest.SuperNetWeight, opt => opt.MapFrom(src =>
                    src.LeafType == "Super" ? src.NetWeight : (decimal?)null));

            // ─── CashRequest ───────────────────────────────────────────────
            CreateMap<CashRequest, CashRequestDto>()
                .ForMember(dest => dest.RequestTime, opt => opt.MapFrom(src =>
                    DateTime.Today.Add(src.RequestTime).ToString("hh:mm tt")))
                .ForMember(dest => dest.SalaryDate, opt => opt.MapFrom(src => src.Month))
                .ForMember(dest => dest.RequestedDate, opt => opt.MapFrom(src =>
                    src.RequestDate.Date.Add(src.RequestTime).ToString("dd MMM yyyy, hh:mm tt")));

            CreateMap<CreateCashRequestDto, CashRequest>()
                .ForMember(dest => dest.RequestNo, opt => opt.Ignore())
                .ForMember(dest => dest.Month, opt => opt.Ignore())
                .ForMember(dest => dest.RequestDate, opt => opt.Ignore())
                .ForMember(dest => dest.RequestTime, opt => opt.Ignore())
                .ForMember(dest => dest.RequestMonth, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore());

            // ─── FertilizerRequest ─────────────────────────────────────────
            CreateMap<FertilizerRequest, FertilizerRequestDto>()
                .ForMember(dest => dest.RequestTime, opt => opt.MapFrom(src =>
                    DateTime.Today.Add(src.RequestTime).ToString("hh:mm tt")))
                .ForMember(dest => dest.SalaryDate, opt => opt.MapFrom(src => src.Month))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src =>
                    src.RequestDate.Date.Add(src.RequestTime).ToString("dd MMM yyyy, hh:mm tt")));

            CreateMap<CreateFertilizerRequestDto, FertilizerRequest>()
                .ForMember(dest => dest.RequestNo, opt => opt.Ignore())
                .ForMember(dest => dest.Month, opt => opt.Ignore())
                .ForMember(dest => dest.RequestDate, opt => opt.Ignore())
                .ForMember(dest => dest.RequestTime, opt => opt.Ignore())
                .ForMember(dest => dest.RequestMonth, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore());

            // ─── ItemRequest ───────────────────────────────────────────────
            CreateMap<ItemRequest, ItemRequestDto>()
                .ForMember(dest => dest.RequestTime, opt => opt.MapFrom(src =>
                    DateTime.Today.Add(src.RequestTime).ToString("hh:mm tt")))
                .ForMember(dest => dest.SalaryDate, opt => opt.MapFrom(src => src.Month))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src =>
                    src.RequestDate.Date.Add(src.RequestTime).ToString("dd MMM yyyy, hh:mm tt")));

            CreateMap<CreateItemRequestDto, ItemRequest>()
                .ForMember(dest => dest.RequestNo, opt => opt.Ignore())
                .ForMember(dest => dest.Month, opt => opt.Ignore())
                .ForMember(dest => dest.RequestDate, opt => opt.Ignore())
                .ForMember(dest => dest.RequestTime, opt => opt.Ignore())
                .ForMember(dest => dest.RequestMonth, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore());

            // ─── Settings mappings ─────────────────────────────────────────
            // FIXED: Complete UserSettings mappings
            CreateMap<UserSettings, UserSettingsDto>()
                .ForMember(dest => dest.Theme, opt => opt.MapFrom(src => src.Theme ?? "light"))
                .ForMember(dest => dest.Language, opt => opt.MapFrom(src => src.Language ?? "english"))
                .ForMember(dest => dest.FontSize, opt => opt.MapFrom(src => src.FontSize ?? 50))
                .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(src => src.ProfileImage))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.BankName, opt => opt.MapFrom(src => src.BankName))
                .ForMember(dest => dest.AccountNumber, opt => opt.MapFrom(src => src.AccountNumber))
                .ForMember(dest => dest.AccountHolder, opt => opt.MapFrom(src => src.AccountHolder))
                .ForMember(dest => dest.Branch, opt => opt.MapFrom(src => src.Branch));

            // FIXED: Only map non-null values from DTO to Entity
            CreateMap<UpdateSettingsDto, UserSettings>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            // Factory mappings
            CreateMap<FactorySetup, FactorySetupDto>();
            CreateMap<FactorySetup, FactoryFeaturesDto>()
                .ForMember(dest => dest.IsCashRequestEnabled, opt => opt.MapFrom(src => src.IsCashRequest))
                .ForMember(dest => dest.IsItemRequestEnabled, opt => opt.MapFrom(src => src.IsItemRequest))
                .ForMember(dest => dest.IsFertilizerRequestEnabled, opt => opt.MapFrom(src => src.IsFertilizerRequest));

            // Notification mappings
            CreateMap<Notification, NotificationDto>()
             .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title ?? string.Empty))
             .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message ?? string.Empty))
             .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type ?? "info"))
             .ForMember(dest => dest.IsRead, opt => opt.Ignore()); // IsRead is calculated, not mapped from entity

            CreateMap<News, NewsDto>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title ?? string.Empty))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content ?? string.Empty))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Content ?? string.Empty))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Content ?? string.Empty));
        }


    }
 }
