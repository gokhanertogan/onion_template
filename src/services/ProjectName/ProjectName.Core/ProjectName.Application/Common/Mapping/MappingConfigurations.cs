using Mapster;
using ProjectName.Application.ProjectName.Common;

namespace ProjectName.Application.Common.Mapping;

public static class MappingConfigurations
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(MappingConfigurations).Assembly);

        TypeAdapterConfig<Domain.Entities.ProjectName, ProjectNameResult>.NewConfig()
           .Map(dest => dest.Name, src => src.Name.ToUpper());
    }
}