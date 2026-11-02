using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Grand.Infrastructure.Plugins;

/// <summary>
///     Reads <see cref="PluginInfoAttribute" /> from assembly metadata. The assembly is never loaded,
///     so a broken plugin cannot affect the process while it is still being inspected.
/// </summary>
public static class PluginManifestReader
{
    private const string CoreAssemblyName = "Grand.Infrastructure";
    private const string AttributeNamespace = "Grand.Infrastructure.Plugins";
    private const string AttributeName = nameof(PluginInfoAttribute);

    /// <summary>
    ///     True when the file is a managed assembly (it carries CLI metadata). A file that is not a
    ///     PE image is not managed; a file that cannot be read throws.
    /// </summary>
    /// <exception cref="IOException">The file cannot be opened or read</exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be opened</exception>
    public static bool IsManagedAssembly(string path)
    {
        using var stream = File.OpenRead(path);
        using var peReader = new PEReader(stream);
        return HasMetadata(peReader);
    }

    /// <summary>
    ///     Returns the manifest of a plugin assembly, or null for a non-managed file or an assembly
    ///     without <see cref="PluginInfoAttribute" />.
    /// </summary>
    /// <exception cref="IOException">The file cannot be opened or read</exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be opened</exception>
    /// <exception cref="BadImageFormatException">The attribute cannot be decoded</exception>
    public static PluginManifest Read(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        if (!HasMetadata(peReader))
            return null;

        var reader = peReader.GetMetadataReader();
        var provider = new StringTypeProvider();
        foreach (var handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(handle);
            if (!IsPluginInfoAttribute(reader, attribute.Constructor))
                continue;

            var named = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var argument in attribute.DecodeValue(provider).NamedArguments)
                if (!named.TryAdd(argument.Name!, argument.Value as string))
                    throw new BadImageFormatException(
                        $"The PluginInfo attribute of '{Path.GetFileName(assemblyPath)}' sets {argument.Name} more than once.");

            return new PluginManifest(
                named.GetValueOrDefault(nameof(PluginInfoAttribute.SystemName)),
                named.GetValueOrDefault(nameof(PluginInfoAttribute.FriendlyName)),
                named.GetValueOrDefault(nameof(PluginInfoAttribute.Group)),
                named.GetValueOrDefault(nameof(PluginInfoAttribute.Author)),
                named.GetValueOrDefault(nameof(PluginInfoAttribute.Version)),
                PluginVersionResolver.ResolveSupportedVersion(GetCoreReference(reader),
                    named.GetValueOrDefault(nameof(PluginInfoAttribute.SupportedVersion))));
        }

        return null;
    }

    /// <summary>
    ///     A file whose headers are not a PE image (a native dll stub, random bytes) has no metadata.
    /// </summary>
    private static bool HasMetadata(PEReader peReader)
    {
        try
        {
            return peReader.HasMetadata;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }

    private static Version GetCoreReference(MetadataReader reader)
    {
        foreach (var handle in reader.AssemblyReferences)
        {
            var reference = reader.GetAssemblyReference(handle);
            if (string.Equals(reader.GetString(reference.Name), CoreAssemblyName, StringComparison.OrdinalIgnoreCase))
                return reference.Version;
        }

        return null;
    }

    private static bool IsPluginInfoAttribute(MetadataReader reader, EntityHandle constructor)
    {
        StringHandle ns, name;
        switch (constructor.Kind)
        {
            case HandleKind.MemberReference:
                var member = reader.GetMemberReference((MemberReferenceHandle)constructor);
                if (member.Parent.Kind != HandleKind.TypeReference)
                    return false;
                var typeRef = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
                ns = typeRef.Namespace;
                name = typeRef.Name;
                break;
            case HandleKind.MethodDefinition:
                var method = reader.GetMethodDefinition((MethodDefinitionHandle)constructor);
                var typeDef = reader.GetTypeDefinition(method.GetDeclaringType());
                ns = typeDef.Namespace;
                name = typeDef.Name;
                break;
            default:
                return false;
        }

        return reader.StringComparer.Equals(ns, AttributeNamespace) &&
               reader.StringComparer.Equals(name, AttributeName);
    }

    /// <summary>
    ///     Only string-valued named arguments are needed, so every type maps to its name.
    /// </summary>
    private sealed class StringTypeProvider : ICustomAttributeTypeProvider<string>
    {
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();

        public string GetSystemType() => "System.Type";

        public string GetSZArrayType(string elementType) => elementType + "[]";

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
            reader.GetString(reader.GetTypeDefinition(handle).Name);

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            reader.GetString(reader.GetTypeReference(handle).Name);

        public string GetTypeFromSerializedName(string name) => name;

        public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;

        public bool IsSystemType(string type) => type == "System.Type";
    }
}
