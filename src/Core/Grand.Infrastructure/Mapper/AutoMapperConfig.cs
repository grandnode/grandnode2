using Grand.Mapping;

namespace Grand.Infrastructure.Mapper;

public static class AutoMapperConfig
{
    private static IMapper? _mapper;

    /// <summary>
    ///     Mapper
    /// </summary>
    public static IMapper Mapper => _mapper ?? throw new InvalidOperationException("AutoMapperConfig.Init has not been called");

    /// <summary>
    ///     Initialize mapper
    /// </summary>
    /// <param name="config">Mapper configuration</param>
    public static void Init(MapperConfiguration config)
    {
        _mapper = config.CreateMapper();
    }
}