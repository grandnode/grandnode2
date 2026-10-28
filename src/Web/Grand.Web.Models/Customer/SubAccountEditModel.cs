using Grand.Domain.Customers;
using Grand.Infrastructure.Models;
using Grand.SharedKernel;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Models.Customer;

public class SubAccountEditModel : BaseEntityModel
{
    [MaxLength(FieldSizeLimits.EmailMaxLength)]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }

    [MaxLength(FieldSizeLimits.NameMaxLength)]
    public string FirstName { get; set; }

    [MaxLength(FieldSizeLimits.NameMaxLength)]
    public string LastName { get; set; }

    public bool Active { get; set; }

    /// <summary>
    ///     Purchasing role of the sub-account
    /// </summary>
    public SubAccountRole Role { get; set; }

    /// <summary>
    ///     Order total above which a buyer's order needs an approval, in the primary store currency, as text in the
    ///     current culture (see <see cref="OrderApprovalLimitText" />); empty means no limit. An approver has none.
    /// </summary>
    public string OrderApprovalLimit { get; set; }

    [DataType(DataType.Password)] public string Password { get; set; }
}