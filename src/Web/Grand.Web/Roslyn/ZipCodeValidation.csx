#! "net5.0"
#r "Grand.Infrastructure"
#r "Grand.Web"
#r "Grand.Web.Common"


using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Common;
using System.Threading.Tasks;
using System;

/* Sample code to validate ZIP Code field in the Address */
public class ZipCodeValidation : IValidatorConsumer<AddressModel>
{
    public void AddRules(BaseGrandValidator<AddressModel> validator)
    {
        validator.RuleFor(x => x.ZipPostalCode).Matches(@"^[0-9]{2}\-[0-9]{3}$")
            .WithMessage("Provided zip code is invalid");
    }
}
