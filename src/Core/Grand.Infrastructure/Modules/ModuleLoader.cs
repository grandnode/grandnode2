using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Modules;

public static class ModuleLoader
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LoadModules(IMvcCoreBuilder mvcCoreBuilder, IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
    {
        var modulesSection = configuration.GetSection("FeatureFlags:Modules");
        if (!modulesSection.Exists())
        {
            return;
        }

        var logger = mvcCoreBuilder.Services.BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger("ModuleManager");

        var modules = modulesSection.Get<Dictionary<string, bool>>();

        foreach (var module in modules)
        {
            if (module.Value)
            {
                var moduleName = module.Key;
                var modulePath = Path.Combine(hostingEnvironment.ContentRootPath, "Modules", moduleName);
                var moduleFile = Path.Combine(modulePath, $"{moduleName}.dll");
                if (File.Exists(moduleFile))
                {
                    try
                    {
                        var assemblyLoadContext = new ModuleLoadContext(Path.Combine(hostingEnvironment.ContentRootPath, "Modules", moduleName));

                        foreach (var dependencyDll in Directory.GetFiles(modulePath, "*.dll"))
                        {
                            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.Location == dependencyDll))
                            {
                                try
                                {
                                    var assembly = assemblyLoadContext.LoadFromAssemblyPath(dependencyDll);
                                    AddApplicationPart(mvcCoreBuilder, assembly);
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                        }
                        logger.LogInformation($"Module '{moduleName}' has been successfully loaded.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error loading module '{moduleName}': {ex.Message}");
                    }
                }
                else
                {
                    logger.LogWarning($"Module '{moduleName}' does not exist in the application directory.");
                }
            }
            else
            {
                logger.LogInformation($"Module '{module.Key}' is disabled.");
            }
        }
    }

    private static void AddApplicationPart(IMvcCoreBuilder mvcCoreBuilder, Assembly assembly)
    {
        mvcCoreBuilder.AddApplicationPart(assembly);

        var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, false);
        foreach (var relatedAssembly in relatedAssemblies)
        {
            var applicationPartFactory = ApplicationPartFactory.GetApplicationPartFactory(relatedAssembly);
            foreach (var part in applicationPartFactory.GetApplicationParts(relatedAssembly))
                mvcCoreBuilder.PartManager.ApplicationParts.Add(part);
        }
    }

    private class ModuleLoadContext : AssemblyLoadContext
    {
        private readonly string _modulePath;

        public ModuleLoadContext(string modulePath) : base(isCollectible: true)
        {
            _modulePath = modulePath;
        }
    }
}