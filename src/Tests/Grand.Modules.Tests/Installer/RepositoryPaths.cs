namespace Grand.Modules.Tests.Installer;

internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();
    public static string InstallerServices => Path.Combine(Root, "src", "Modules", "Grand.Module.Installer", "Services");
    public static string Samples => Path.Combine(Root, "src", "Web", "Grand.Web", "wwwroot", "assets", "samples");

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "GrandNode.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("GrandNode.slnx not found above test output");
    }
}
