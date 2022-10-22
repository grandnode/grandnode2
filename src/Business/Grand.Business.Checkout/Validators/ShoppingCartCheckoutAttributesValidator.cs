using FluentValidation;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;

namespace Grand.Business.Checkout.Validators
{

    public record ShoppingCartCheckoutAttributesValidatorRecord(Customer Customer, Store Store, IList<ShoppingCartItem> ShoppingCarts, IList<CustomAttribute> CheckoutAttributes);

    public class ShoppingCartCheckoutAttributesValidator : AbstractValidator<ShoppingCartCheckoutAttributesValidatorRecord>
    {
        public ShoppingCartCheckoutAttributesValidator(ITranslationService translationService,
            ICheckoutAttributeParser checkoutAttributeParser, ICheckoutAttributeService checkoutAttributeService)
        {

            RuleFor(x => x).CustomAsync(async (value, context, ct) =>
            {
                //selected attributes
                var attributes1 = await checkoutAttributeParser.ParseCheckoutAttributes(value.CheckoutAttributes);

                //existing checkout attributes
                var attributes2 = await checkoutAttributeService.GetAllCheckoutAttributes(value.Store.Id, !value.ShoppingCarts.RequiresShipping());
                foreach (var a2 in attributes2)
                {
                    var conditionMet = await checkoutAttributeParser.IsConditionMet(a2, value.CheckoutAttributes);
                    if (a2.IsRequired && ((conditionMet.HasValue && conditionMet.Value) || !conditionMet.HasValue))
                    {
                        bool found = false;
                        //selected checkout attributes
                        foreach (var a1 in attributes1)
                        {
                            if (a1.Id == a2.Id)
                            {
                                var attributeValuesStr = value.CheckoutAttributes.Where(x => x.Key == a1.Id).Select(x => x.Value);
                                foreach (var str1 in attributeValuesStr)
                                    if (!string.IsNullOrEmpty(str1.Trim()))
                                    {
                                        found = true;
                                        break;
                                    }
                            }
                        }

                        //if not found
                        if (!found)
                        {
                            context.AddFailure(a2.TextPrompt ?? string.Format(translationService.GetResource("ShoppingCart.SelectAttribute"), a2.Name));                            
                        }
                    }
                }

                //now validation rules

                //minimum length
                foreach (var ca in attributes2)
                {
                    if (ca.ValidationMinLength.HasValue)
                    {
                        if (ca.AttributeControlTypeId == AttributeControlType.TextBox ||
                            ca.AttributeControlTypeId == AttributeControlType.MultilineTextbox)
                        {
                            var valuesStr = value.CheckoutAttributes.Where(x => x.Key == ca.Id).Select(x => x.Value);
                            var enteredText = valuesStr?.FirstOrDefault();
                            int enteredTextLength = string.IsNullOrEmpty(enteredText) ? 0 : enteredText.Length;

                            if (ca.ValidationMinLength.Value > enteredTextLength)
                            {
                                context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.TextboxMinimumLength"), ca.Name, ca.ValidationMinLength.Value));
                            }
                        }
                    }

                    //maximum length
                    if (ca.ValidationMaxLength.HasValue)
                    {
                        if (ca.AttributeControlTypeId == AttributeControlType.TextBox ||
                            ca.AttributeControlTypeId == AttributeControlType.MultilineTextbox)
                        {
                            var valuesStr = value.CheckoutAttributes.Where(x => x.Key == ca.Id).Select(x => x.Value);
                            var enteredText = valuesStr?.FirstOrDefault();
                            int enteredTextLength = string.IsNullOrEmpty(enteredText) ? 0 : enteredText.Length;

                            if (ca.ValidationMaxLength.Value < enteredTextLength)
                            {
                                context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.TextboxMaximumLength"), ca.Name, ca.ValidationMaxLength.Value));
                            }
                        }
                    }
                }
            });
        }
    }
}
