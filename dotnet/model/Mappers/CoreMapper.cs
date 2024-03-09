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
            .ReverseMap();
    }
}
