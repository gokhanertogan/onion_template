using Mapster;
using ServiceName.Application.ServiceName.Common;

namespace ServiceName.Application.Common.Mapping;

public static class MappingConfigurations
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(MappingConfigurations).Assembly);

        TypeAdapterConfig<Domain.Entities.ServiceName, ServiceNameResult>.NewConfig()
           .Map(dest => dest.Name, src => src.Name.ToUpper());
    }
}