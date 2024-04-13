using Grand.Infrastructure.Models;
using Grand.SharedKernel;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Models.Customer;

public class SubAccountCreateModel : BaseEntityModel
{
    [MaxLength(FieldSizeLimits.EmailMaxLength)]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; }

    [MaxLength(FieldSizeLimits.NameMaxLength)]
    public string FirstName { get; set; }

    [MaxLength(FieldSizeLimits.NameMaxLength)]
    public string LastName { get; set; }

    public bool Active { get; set; }

    [DataType(DataType.Password)] public string Password { get; set; }
}