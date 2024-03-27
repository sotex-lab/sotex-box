using System.Net;
using AutoMapper;
using model.Contracts;
using model.Core;

namespace model.Mappers;

public class CoreMapper : Profile
{
    public CoreMapper()
    {
        CreateMap<Ad, AdContract>()
            .ForMember(dest => dest.Scope, opt => opt.MapFrom(src => src.AdScope))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(t => t.Name)))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));

        CreateMap<AdContract, Ad>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AdScope, opt => opt.MapFrom(src => src.Scope))
            .ForMember(
                dest => dest.Tags,
                opt => opt.MapFrom(src => src.Tags.Select(tagName => new Tag { Name = tagName }))
            );

        CreateMap<Device, DeviceContract>(MemberList.Destination)
            .ForMember(
                dest => dest.Ip,
                opt => opt.MapFrom(src => src.Ip == null ? "" : src.Ip.ToString())
            );

        CreateMap<DeviceContract, Device>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UtilityName, opt => opt.MapFrom(src => src.UtilityName))
            .ForMember(dest => dest.Ip, opt => opt.Ignore());
    }
}
