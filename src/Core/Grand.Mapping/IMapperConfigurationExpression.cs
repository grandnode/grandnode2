namespace Grand.Mapping;

public interface IMapperConfigurationExpression
{
    void AddProfile(Profile profile);
    void AddProfile(Type profileType);
}
