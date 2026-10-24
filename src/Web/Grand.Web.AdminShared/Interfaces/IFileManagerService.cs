using Grand.Web.AdminShared.Models.FileManager;

namespace Grand.Web.AdminShared.Interfaces;

/// <summary>
///     The files the rich text editor picks from: a folder tree under assets/images/uploaded. Every path is relative to
///     that root and never leaves it.
/// </summary>
public interface IFileManagerService
{
    FileManagerListing List(string path);
    Task<FileManagerUploadResult> Upload(string path, IReadOnlyList<FileManagerUpload> files);
    Task<FileManagerResult> CreateFolder(string path, string name);
    Task<FileManagerResult> Rename(string path, string name);
    Task<FileManagerResult> Delete(string path);
}
