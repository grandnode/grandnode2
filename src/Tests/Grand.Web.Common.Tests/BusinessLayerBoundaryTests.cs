using Grand.Business.Core.Interfaces.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Grand.Web.Common.Tests;

[TestClass]
public class BusinessLayerBoundaryTests
{
    //Data Protection is a host-agnostic library despite its namespace (OidcSessionService protects tokens with it)
    private const string AllowedAspNetCorePrefix = "Microsoft.AspNetCore.DataProtection";

    private static readonly Assembly[] BusinessAssemblies =
    [
        typeof(IMediaFileStore).Assembly,
        typeof(Business.Authentication.Startup.StartupApplication).Assembly,
        typeof(Business.Catalog.Startup.StartupApplication).Assembly,
        typeof(Business.Checkout.Startup.StartupApplication).Assembly,
        typeof(Business.Cms.Startup.StartupApplication).Assembly,
        typeof(Business.Common.Startup.StartupApplication).Assembly,
        typeof(Business.Customers.Startup.StartupApplication).Assembly,
        typeof(Business.Marketing.Startup.StartupApplication).Assembly,
        typeof(Business.Messages.Startup.StartupApplication).Assembly,
        typeof(Business.Storage.Startup.StartupApplication).Assembly
    ];

    [TestMethod]
    public void BusinessAssemblies_DoNotReferenceAspNetCore()
    {
        var violations = BusinessAssemblies
            .SelectMany(assembly => assembly.GetReferencedAssemblies()
                .Where(reference => reference.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) &&
                                    !reference.Name.StartsWith(AllowedAspNetCorePrefix, StringComparison.Ordinal))
                .Select(reference => $"{assembly.GetName().Name} -> {reference.Name}"))
            .ToList();

        Assert.IsEmpty(violations, string.Join(Environment.NewLine, violations));
    }
}
