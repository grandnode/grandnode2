namespace Grand.Mapping;

public interface IMapperConfigurationExpression
{
    void AddProfile(Profile profile);
    void AddProfile<T>() where T : Profile, new() => AddProfile(new T());
}
