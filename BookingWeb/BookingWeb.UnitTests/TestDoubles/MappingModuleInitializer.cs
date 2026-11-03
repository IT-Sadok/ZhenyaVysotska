using System.Runtime.CompilerServices;
using BookingWeb.Application.Common.Mapping;

namespace BookingWeb.UnitTests.TestDoubles;

internal static class MappingModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        MappingConfig.RegisterMappings();
    }
}
