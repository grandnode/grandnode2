using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Common.Models;

public class DatePickerDropDownsModel : BaseEntityModel
{
    public string Attribute { get; set; }

    public string Day { get; set; }

    public IList<SelectListItem> SelectListDay { get; set; } = new List<SelectListItem>();

    public string Month { get; set; }
    public IList<SelectListItem> SelectListMonth { get; set; } = new List<SelectListItem>();

    public string Year { get; set; }
    public IList<SelectListItem> SelectListYear { get; set; } = new List<SelectListItem>();

    public int Begin_Year { get; set; }

    public int End_Year { get; set; }

    public int SelectedDay { get; set; }

    public int SelectedMonth { get; set; }

    public int SelectedYear { get; set; }
}