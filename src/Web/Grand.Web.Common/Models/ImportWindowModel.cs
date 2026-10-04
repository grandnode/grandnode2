using Microsoft.AspNetCore.Html;

namespace Grand.Web.Common.Models;

/// <summary>
///     The one shape every "pick a file and import it" window in the panels is built from.
///     Rendered by <c>Partials/ImportWindow</c>, filled in by the
///     <c>&lt;admin-import-window&gt;</c> tag helper.
/// </summary>
public class ImportWindowModel
{
    /// <summary>Id of the hidden element the modal is opened on.</summary>
    public string WindowId { get; set; }

    /// <summary>Area, controller and action the form posts to.</summary>
    public string AreaName { get; set; }

    public string ControllerName { get; set; }

    public string ActionName { get; set; }

    /// <summary>Route id of the edited record, for an import that belongs to one.</summary>
    public string RouteId { get; set; }

    /// <summary>Posted name of the file field - part of the server contract.</summary>
    public string FileName { get; set; }

    /// <summary>Element id of the file field; defaults to <see cref="FileName" />.</summary>
    public string FileId { get; set; }

    /// <summary>Label of the file field.</summary>
    public string FileLabel { get; set; }

    /// <summary>Value of the field's accept attribute, when the import takes one format.</summary>
    public string Accept { get; set; }

    /// <summary>Caption and element id of the submit button.</summary>
    public string SubmitText { get; set; }

    public string SubmitId { get; set; }

    /// <summary>Whatever the view puts above the field: the tip, the notes.</summary>
    public IHtmlContent Notes { get; set; }
}
