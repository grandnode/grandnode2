using System.Text;

namespace Grand.Business.Storage.Services;

/// <summary>
///     Writes a file so a concurrent reader - another instance on a shared volume, or the static file
///     middleware - sees either the old or the new content, never a half-written file
/// </summary>
public static class AtomicFile
{
    public static async Task WriteAllBytesAsync(string path, byte[] bytes)
    {
        var tempPath = TempPathFor(path);
        try
        {
            await File.WriteAllBytesAsync(tempPath, bytes);
            await Replace(tempPath, path);
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
            await Replace(tempPath, path);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    //same directory, so the rename never crosses a file system
    private static string TempPathFor(string path) => $"{path}.{Guid.NewGuid():N}.tmp";

    //Windows refuses to rename over a file a reader holds open; wait for the reader instead of overwriting in
    //place, which would expose a half-written file
    private const int ReplaceAttempts = 20;
    private static readonly TimeSpan ReplaceRetryDelay = TimeSpan.FromMilliseconds(50);

    private static async Task Replace(string tempPath, string path)
    {
        for (var attempt = 1;; attempt++)
            try
            {
                File.Move(tempPath, path, true);
                return;
            }
            catch (Exception ex) when (attempt < ReplaceAttempts && IsSharingViolation(ex))
            {
                await Task.Delay(ReplaceRetryDelay);
            }
    }

    private static bool IsSharingViolation(Exception ex)
    {
        //ERROR_SHARING_VIOLATION (32) / ERROR_LOCK_VIOLATION (33) come as IOException, ERROR_ACCESS_DENIED as
        //UnauthorizedAccessException when the target is open
        return ex is UnauthorizedAccessException ||
               (ex is IOException && (ex.HResult & 0xFFFF) is 32 or 33);
    }
}
