using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Views;

/// <summary>
///     Razor turns <c>&lt;vc:...&gt;</c> into a tag helper only for the view components of an assembly
///     the view registers with <c>@addTagHelper</c>. Any other <c>&lt;vc:...&gt;</c> compiles without a
///     warning and reaches the browser as an empty unknown element, so the component never runs.
/// </summary>
[TestClass]
public class ViewComponentTagTests
{
    private static readonly string Src = Path.Combine(Installer.RepositoryPaths.Root, "src");
    private static readonly HashSet<string> SkippedFolders = ["bin", "obj", "node_modules", "wwwroot"];

    [TestMethod]
    public void EveryVcTagResolvesToAComponentItsViewRegisters()
    {
        var components = ComponentAssemblies();
        var problems = new List<string>();
        var checkedTags = 0;

        foreach (var view in SourceFiles(Src, "*.cshtml"))
        {
            var tags = Regex.Matches(File.ReadAllText(view), "<vc:([a-z0-9-]+)")
                .Select(m => m.Groups[1].Value).Distinct().ToList();
            if (tags.Count == 0) continue;

            var project = ProjectOf(view);
            var registered = RegisteredAssemblies(view, project.Dir);
            foreach (var tag in tags)
            {
                checkedTags++;
                if (!components.TryGetValue(tag, out var assemblies))
                    problems.Add($"{Path.GetRelativePath(Src, view)}: <vc:{tag}> matches no view component");
                else if (!assemblies.Overlaps(registered))
                    problems.Add($"{Path.GetRelativePath(Src, view)}: <vc:{tag}> lives in {string.Join(", ", assemblies)}, " +
                                 $"which {project.Name} does not register with @addTagHelper");
            }
        }

        Assert.IsTrue(checkedTags > 0, "No <vc:...> tag found, so the scan itself is broken");
        Assert.AreEqual(0, problems.Count, Environment.NewLine + string.Join(Environment.NewLine, problems));
    }

    [TestMethod]
    public void ComponentScan_FindsWidgetComponentsOfEveryPanel()
    {
        var components = ComponentAssemblies();

        CollectionAssert.AreEquivalent(new[] { "Grand.Web.Common" }, components["widget"].ToList());
        CollectionAssert.AreEquivalent(new[] { "Grand.Web.Admin" }, components["admin-widget"].ToList());
        CollectionAssert.AreEquivalent(new[] { "Grand.Web.Store" }, components["store-widget"].ToList());
        CollectionAssert.AreEquivalent(new[] { "Grand.Web.Vendor" }, components["vendor-widget"].ToList());
        CollectionAssert.AreEquivalent(new[] { "Grand.Web" }, components["related-products"].ToList());
    }

    //tag name -> assemblies that declare a view component with that name
    private static Dictionary<string, HashSet<string>> ComponentAssemblies()
    {
        var declaration = new Regex(
            @"(?:\[ViewComponent\(Name\s*=\s*""(?<name>[^""]+)""\)\]\s*)?public\s+(?:sealed\s+)?class\s+(?<class>\w+)(?:\s*:\s*(?<base>\w+))?");
        var components = new Dictionary<string, HashSet<string>>();
        foreach (var file in SourceFiles(Src, "*.cs").Where(f => !f.Contains($"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}")))
        foreach (Match m in declaration.Matches(File.ReadAllText(file)))
        {
            var className = m.Groups["class"].Value;
            var isComponent = m.Groups["name"].Success || className.EndsWith("ViewComponent") ||
                              m.Groups["base"].Value.EndsWith("ViewComponent");
            if (!isComponent) continue;

            var name = m.Groups["name"].Success ? m.Groups["name"].Value : className[..^"ViewComponent".Length];
            var tag = Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", "-").ToLowerInvariant();
            if (!components.TryGetValue(tag, out var assemblies))
                components[tag] = assemblies = [];
            assemblies.Add(ProjectOf(file).Name);
        }

        return components;
    }

    private static HashSet<string> RegisteredAssemblies(string view, string projectDir)
    {
        var assemblies = new HashSet<string>();
        for (var dir = Path.GetDirectoryName(view); dir != null && dir.Length >= projectDir.Length; dir = Path.GetDirectoryName(dir))
        {
            var imports = Path.Combine(dir, "_ViewImports.cshtml");
            if (!File.Exists(imports)) continue;
            foreach (Match m in Regex.Matches(File.ReadAllText(imports), @"@addTagHelper\s+\*\s*,\s*([\w.]+)"))
                assemblies.Add(m.Groups[1].Value);
        }

        return assemblies;
    }

    private static (string Dir, string Name) ProjectOf(string file)
    {
        for (var dir = Path.GetDirectoryName(file); dir != null; dir = Path.GetDirectoryName(dir))
        {
            var project = Directory.GetFiles(dir, "*.csproj").FirstOrDefault();
            if (project != null) return (dir, Path.GetFileNameWithoutExtension(project));
        }

        throw new InvalidOperationException($"{file} belongs to no project");
    }

    private static IEnumerable<string> SourceFiles(string dir, string pattern)
    {
        foreach (var file in Directory.GetFiles(dir, pattern))
            yield return file;
        foreach (var sub in Directory.GetDirectories(dir).Where(d => !SkippedFolders.Contains(Path.GetFileName(d))))
        foreach (var file in SourceFiles(sub, pattern))
            yield return file;
    }
}
