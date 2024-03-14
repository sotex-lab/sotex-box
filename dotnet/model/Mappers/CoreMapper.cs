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
            .ForMember(dest => dest.AdScope, opt => opt.MapFrom(src => src.Scope))
            .ForMember(
                dest => dest.Tags,
                opt => opt.MapFrom(src => src.Tags.Select(tagName => new Tag { Name = tagName }))
            );
    }
}
