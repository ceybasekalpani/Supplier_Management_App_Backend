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
                .ForMember(dest => dest.Gross, opt => opt.MapFrom(src => src.GrossQty ?? 0))
                .ForMember(dest => dest.Bags, opt => opt.MapFrom(src => src.NoofBags ?? 0))
                .ForMember(dest => dest.Water, opt => opt.MapFrom(src => src.WaterWgt ?? 0))
                .ForMember(dest => dest.NetWeight, opt => opt.MapFrom(src =>
                    (src.Qty ?? 0) - (src.WaterWgt ?? 0)))
                .ForMember(dest => dest.SuperNetWeight, opt => opt.MapFrom(src =>
                    src.LeafType == "Super" ? (src.Qty ?? 0) - (src.WaterWgt ?? 0) : (decimal?)null));

            // Request mappings
            CreateMap<CashRequest, CashRequestDto>()
                .ForMember(dest => dest.RequestTime, opt => opt.MapFrom(src => src.RequestTime.ToString(@"hh\:mm")))
                .ForMember(dest => dest.RequestedDate, opt => opt.MapFrom(src =>
                    src.RequestDate.ToString("dd MMM yyyy, HH:mm")));

            CreateMap<CreateCashRequestDto, CashRequest>()
                .ForMember(dest => dest.RequestNo, opt => opt.Ignore())
                .ForMember(dest => dest.RequestDate, opt => opt.MapFrom(src => DateTime.UtcNow.Date))
                .ForMember(dest => dest.RequestTime, opt => opt.MapFrom(src => DateTime.UtcNow.TimeOfDay))
                .ForMember(dest => dest.RequestMonth, opt => opt.MapFrom(src =>
                    DateTime.ParseExact(src.Month, "MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "pending"));

            CreateMap<FertilizerRequest, FertilizerRequestDto>()
                .ForMember(dest => dest.RequestTime, opt => opt.MapFrom(src => src.RequestTime.ToString(@"hh\:mm")))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src =>
                    src.RequestDate.ToString("dd MMM yyyy, HH:mm")));

            CreateMap<CreateFertilizerRequestDto, FertilizerRequest>()
                .ForMember(dest => dest.RequestNo, opt => opt.Ignore())
                .ForMember(dest => dest.RequestDate, opt => opt.MapFrom(src => DateTime.UtcNow.Date))
                .ForMember(dest => dest.RequestTime, opt => opt.MapFrom(src => DateTime.UtcNow.TimeOfDay))
                .ForMember(dest => dest.RequestMonth, opt => opt.MapFrom(src =>
                    DateTime.ParseExact(src.Month, "MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "pending"));

            CreateMap<ItemRequest, ItemRequestDto>()
                .ForMember(dest => dest.RequestTime, opt => opt.MapFrom(src => src.RequestTime.ToString(@"hh\:mm")))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src =>
                    src.RequestDate.ToString("dd MMM yyyy, HH:mm")));

            CreateMap<CreateItemRequestDto, ItemRequest>()
                .ForMember(dest => dest.RequestNo, opt => opt.Ignore())
                .ForMember(dest => dest.RequestDate, opt => opt.MapFrom(src => DateTime.UtcNow.Date))
                .ForMember(dest => dest.RequestTime, opt => opt.MapFrom(src => DateTime.UtcNow.TimeOfDay))
                .ForMember(dest => dest.RequestMonth, opt => opt.MapFrom(src =>
                    DateTime.ParseExact(src.Month, "MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "pending"));

            // Settings mappings
            CreateMap<UserSettings, UserSettingsDto>();
            CreateMap<UpdateSettingsDto, UserSettings>();

            // Factory mappings
            CreateMap<FactorySetup, FactorySetupDto>();
            CreateMap<FactorySetup, FactoryFeaturesDto>()
                .ForMember(dest => dest.IsCashRequestEnabled, opt => opt.MapFrom(src => src.IsCashRequest))
                .ForMember(dest => dest.IsItemRequestEnabled, opt => opt.MapFrom(src => src.IsItemRequest))
                .ForMember(dest => dest.IsFertilizerRequestEnabled, opt => opt.MapFrom(src => src.IsFertilizerRequest));

            // Notification mappings
            CreateMap<Notification, NotificationDto>();
            CreateMap<News, NewsDto>();
        }
    }
}