namespace Grand.Mapping.Internal;

internal interface IMappingConfiguration
{
    (Type Source, Type Dest) GetTypes();
    Delegate CompileDelegate(HashSet<(Type, Type)> registeredTypes, Dictionary<(Type, Type), Delegate> mappings);
}
