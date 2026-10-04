namespace Grand.Mapping.Internal;

internal sealed class MappingConfiguration<TSource, TDest> : IMappingConfiguration
{
    private readonly MappingExpressionImpl<TSource, TDest> _expression;

    public MappingConfiguration(MappingExpressionImpl<TSource, TDest> expression)
        => _expression = expression;

    public (Type Source, Type Dest) GetTypes() => (typeof(TSource), typeof(TDest));

    public Delegate CompileDelegate(HashSet<(Type, Type)> registeredTypes, Dictionary<(Type, Type), Delegate> mappings)
        => MappingCompiler.Compile<TSource, TDest>(_expression.MemberConfigs, registeredTypes, mappings);
}
