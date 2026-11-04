using Grand.Infrastructure.Plugins;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Infrastructure;

/// <summary>
///     A real built plugin, loaded into a context of its own as the application loads it: MVC must still find its
///     controllers and its precompiled views. Inconclusive when the plugin has not been built.
/// </summary>
[TestClass]
public class PluginLoadContextApplicationPartsTests
{
    private const string PluginName = "Widgets.Slider";

    [TestMethod]
    public void BuiltPlugin_InItsOwnContext_ControllersAndCompiledViewsAreDiscovered()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Web",
            "Grand.Web", "Plugins", PluginName, PluginName + ".dll"));
        if (!File.Exists(path))
            Assert.Inconclusive($"{path} is not built; run dotnet build src/Plugins/{PluginName}");

        var context = new IsolatedLoadContext(PluginName, path, PluginManager.HostAssemblyNames(), true);
        try
        {
            var assembly = context.LoadFromAssemblyPath(path);
            //as PluginManager adds an installed plugin; the Razor view engine brings the compiled-views provider
            var partManager = new ServiceCollection().AddMvcCore().AddRazorViewEngine().AddApplicationPart(assembly)
                .PartManager;

            var controllers = new ControllerFeature();
            partManager.PopulateFeature(controllers);
            var views = new ViewsFeature();
            partManager.PopulateFeature(views);

            Assert.IsTrue(controllers.Controllers.Any(x => x.Assembly == assembly), "no controller of the plugin");
            var pluginViews = views.ViewDescriptors.Where(x => x.Type?.Assembly == assembly).ToList();
            Assert.IsNotEmpty(pluginViews, "no compiled view of the plugin");
            CollectionAssert.IsSubsetOf(
                new[] {
                    "/Areas/Admin/Views/WidgetsSlider/Configure.cshtml",
                    "/Views/Shared/Components/WidgetSlider/Default.cshtml"
                },
                pluginViews.Select(x => x.RelativePath).ToList());
            //the host's MVC types, not copies: the context took them from the default one
            Assert.IsTrue(controllers.Controllers.Where(x => x.Assembly == assembly)
                .All(x => typeof(ControllerBase).IsAssignableFrom(x)));
            //Razor runtime compilation (Development) reads every part's references through the plugin's deps.json,
            //which the plugin folder now keeps
            foreach (var part in partManager.ApplicationParts.OfType<ICompilationReferencesProvider>())
                _ = part.GetReferencePaths().ToList();
        }
        finally
        {
            context.Unload();
        }
    }
}
