using elFinder.Net.Core.Models.FileInfo;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Writes elFinder file info by its runtime type.
///     OpenResponse.cwd is declared as BaseInfoResponse while the instance is a DirectoryInfoResponse
///     or a RootInfoResponse, and System.Text.Json serializes the declared type - so phash, volumeid
///     and dirs never reach the browser. elFinder caches cwd in its file map, overwriting the complete
///     entry it got from files[] with a parentless one, and can no longer walk a file back to the
///     volume root. It then builds URLs as volume url + file name, dropping every subdirectory, so
///     picking a picture out of a subfolder yields a 404.
/// </summary>
public class BaseInfoResponseConverter : JsonConverter<BaseInfoResponse>
{
    /// <summary>
    ///     Only the declared base type is intercepted; the nested Write call resolves the runtime type
    ///     through the default converter and does not re-enter this one.
    /// </summary>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(BaseInfoResponse);
    }

    public override BaseInfoResponse Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException("The elFinder connector response is write-only");
    }

    public override void Write(Utf8JsonWriter writer, BaseInfoResponse value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
