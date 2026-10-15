namespace Grand.Business.Catalog.Services.ExportImport;

/// <summary>Helpers the catalogue schema properties share</summary>
internal static class ExportPictures
{
    /// <summary>
    ///     Loads each distinct picture once, before the rows are written, keyed by picture id. An empty id is left out, so
    ///     an entity without a picture exports an empty cell instead of the default picture's path, which an import would
    ///     read back as a picture to download. A cancelled token stops before the next picture.
    /// </summary>
    public static async Task<Dictionary<string, string>> Preload(IEnumerable<string> pictureIds,
        Func<string, Task<string>> getPicture, CancellationToken cancellationToken = default)
    {
        var pictures = new Dictionary<string, string>();
        foreach (var pictureId in pictureIds.Where(x => !string.IsNullOrEmpty(x)).Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            pictures[pictureId] = await getPicture(pictureId);
        }
        return pictures;
    }
}
