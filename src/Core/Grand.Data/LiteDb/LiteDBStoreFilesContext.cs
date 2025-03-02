using Grand.Domain;
using LiteDB;

namespace Grand.Data.LiteDb;

public class LiteDBStoreFilesContext : IStoreFilesContext
{
    private readonly LiteDatabase _database;

    public LiteDBStoreFilesContext(LiteDatabase database)
    {
        _database = database;
    }

    public async Task<byte[]> BucketDownload(string id)
    {
        var fs = _database.FileStorage;
        var file = fs.FindById(id);

        ArgumentNullException.ThrowIfNull(file);

        using (var stream = file.OpenRead())
        using (MemoryStream mstream = new())
        {
            await stream.CopyToAsync(mstream);
            return mstream.ToArray();
        }
    }

    public Task BucketDelete(string id)
    {
        var fs = _database.FileStorage;
        fs.Delete(id);

        return Task.CompletedTask;
    }

    public async Task<string> BucketUploadFromBytes(string filename, byte[] source)
    {
        var id = UniqueIdentifier.New;
        var fs = _database.FileStorage;
        fs.Upload(id, filename, new MemoryStream(source));

        return await Task.FromResult(id);
    }
}