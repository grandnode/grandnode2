﻿using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Configuration;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace Grand.Infrastructure.Roslyn
{
    public static class RoslynCompiler
    {
        #region Const

        public const string ScriptPath = "Roslyn";
        public const string ShadowCopyScriptPath = "Roslyn/bin";

        #endregion

        #region Fields

        private static DirectoryInfo _shadowCopyScriptPath;

        #endregion

        public static void Load(ApplicationPartManager applicationPartManager, IConfiguration configuration)
        {
            var config = new ExtensionsConfig();
            configuration.GetSection("Extensions").Bind(config);

            if (applicationPartManager == null)
                throw new ArgumentNullException(nameof(applicationPartManager));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!config.UseRoslynScripts)
                return;

            var referencedScripts = new List<ResultCompiler>();

            var roslynFolder = new DirectoryInfo(CommonPath.MapPath(ScriptPath));

            _shadowCopyScriptPath = new DirectoryInfo(CommonPath.MapPath(ShadowCopyScriptPath));
            Directory.CreateDirectory(_shadowCopyScriptPath.FullName);

            //clear bin files
            var binFiles = _shadowCopyScriptPath.GetFiles("*", SearchOption.AllDirectories);
            foreach (var f in binFiles)
            {
                try
                {
                    //ignore index.htm
                    var fileName = Path.GetFileName(f.FullName);
                    if (fileName.Equals("index.htm", StringComparison.OrdinalIgnoreCase))
                        continue;

                    File.Delete(f.FullName);
                }
                catch (Exception exc)
                {
                    Debug.WriteLine("Error deleting file " + f.Name + ". Exception: " + exc);
                }
            }

            try
            {
                var ctxFiles = roslynFolder.GetFiles("*.csx", SearchOption.TopDirectoryOnly);
                foreach (var file in ctxFiles)
                {
                    var csxcript = new ResultCompiler
                    {
                        OriginalFile = file.FullName
                    };

                    string ctxCode = File.ReadAllText(file.FullName);
                    var sourceFileResolver = new SourceFileResolver(ImmutableArray<string>.Empty, AppContext.BaseDirectory);
                    var opts = ScriptOptions.Default.WithSourceResolver(sourceFileResolver);
                    var script = CSharpScript.Create(ctxCode, opts);
                    var compilation = script.GetCompilation();
                    using (var ms = new MemoryStream())
                    {
                        var compilationResult = compilation.Emit(ms);

                        if (compilationResult.Success)
                        {
                            ms.Seek(0, SeekOrigin.Begin);
                            var shadowFileName = Path.Combine(_shadowCopyScriptPath.FullName, file.Name + "-" + Guid.NewGuid().ToString("D") + ".dll");
                            File.WriteAllBytes(shadowFileName, ms.ToArray());
                            Assembly shadowCopiedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(shadowFileName);
                            csxcript.DLLAssemblyFile = shadowFileName;
                            csxcript.ReferencedAssembly = shadowCopiedAssembly;
                            csxcript.IsCompiled = true;
                            applicationPartManager.ApplicationParts.Add(new AssemblyPart(shadowCopiedAssembly));
                        }
                        else
                        {
                            foreach (var diagnostic in compilationResult.Diagnostics)
                            {
                                csxcript.ErrorInfo.Add(diagnostic.ToString());
                            }
                        }
                    }
                    referencedScripts.Add(csxcript);

                }
                ReferencedScripts = referencedScripts;
            }
            catch (Exception ex)
            {
                var msg = string.Format("Roslyn '{0}'", ex.Message);

                var fail = new Exception(msg, ex);
                throw fail;
            }
        }

        /// <summary>
        /// Returns a collection of all referenced assemblies 
        /// </summary>
        public static IEnumerable<ResultCompiler> ReferencedScripts { get; set; }

        /// <summary>
        /// Method for compiling the code for testing in admin panel
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static ResultCompiler ResultCompiledScript(string code)
        {
            var result = new ResultCompiler();
            var sourceFileResolver = new SourceFileResolver(ImmutableArray<string>.Empty, AppContext.BaseDirectory);
            var opts = ScriptOptions.Default.WithSourceResolver(sourceFileResolver);
            var script = CSharpScript.Create(code, opts);
            var compilation = script.GetCompilation();
            using (var ms = new MemoryStream())
            {
                var compilationResult = compilation.Emit(ms);
                if (compilationResult.Success)
                {
                    result.IsCompiled = true;
                }
                else
                {
                    foreach (var diagnostic in compilationResult.Diagnostics)
                    {
                        result.ErrorInfo.Add(diagnostic.ToString());
                    }
                }
            }
            return result;
        }
    }
}
