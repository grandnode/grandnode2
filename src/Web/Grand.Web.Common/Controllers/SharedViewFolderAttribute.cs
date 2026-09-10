namespace Grand.Web.Common.Controllers;

/// <summary>
///     Applied to an abstract base controller (in Grand.Web.AdminShared) whose concrete per-host
///     subclasses are named differently than the view folder their .cshtml files still live under —
///     needed when one host's original controller file is split into several entity-specific base-
///     controller-derived classes that each keep the original controller's view folder via routing
///     but not via C# class naming. Inherited (default), so applying this once on the abstract base
///     covers every concrete subclass in every host without repeating it.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class SharedViewFolderAttribute(string controllerName) : Attribute
{
    /// <summary>The view-folder name Razor should search under (e.g. "Shipping"), NOT the
    /// controller's own C# class-derived name.</summary>
    public string ControllerName { get; } = controllerName;
}
