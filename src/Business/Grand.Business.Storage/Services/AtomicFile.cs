using System.Text;

namespace Grand.Business.Storage.Services;

/// <summary>
///     Writes a file so a concurrent reader - another instance on a shared volume, or the static file
///     middleware - sees either the old or the new content, never a half-written file
/// </summary>
public static class AtomicFile
{
    public static void WriteAllBytes(string path, byte[] bytes)
    {
        var tempPath = TempPathFor(path);
        try
        {
            File.WriteAllBytes(tempPath, bytes);
            Replace(tempPath, path);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public static async Task WriteAllTextAsync(string path, string text)
    {
        var tempPath = TempPathFor(path);
        try
        {
            await File.WriteAllTextAsync(tempPath, text, Encoding.UTF8);
            Replace(tempPath, path);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    //same directory, so the rename never crosses a file system
    private static string TempPathFor(string path) => $"{path}.{Guid.NewGuid():N}.tmp";

    private static void Replace(string tempPath, string path)
    {
        try
        {
            File.Move(tempPath, path, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            //Windows refuses to replace a file another process holds open; overwrite it in place instead
            File.Copy(tempPath, path, true);
        }
    }
}
