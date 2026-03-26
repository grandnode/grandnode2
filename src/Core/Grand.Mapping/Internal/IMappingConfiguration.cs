namespace Grand.Mapping.Internal;

internal interface IMappingConfiguration
{
    (Type Source, Type Dest) GetTypes();
    Delegate CompileDelegate();
}
