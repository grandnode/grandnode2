namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual Task InstallCollections() => Task.CompletedTask;
}
