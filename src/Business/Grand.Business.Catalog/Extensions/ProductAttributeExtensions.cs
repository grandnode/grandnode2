using Grand.Domain.Catalog;

namespace Grand.Business.Catalog.Extensions
{
    public static class ProductAttributeExtensions
    {
        public static bool ShouldHaveValues(this ProductAttributeMapping productAttributeMapping)
        {
            if (productAttributeMapping == null)
                return false;

            if (productAttributeMapping.AttributeControlTypeId == AttributeControlType.TextBox ||
                productAttributeMapping.AttributeControlTypeId == AttributeControlType.MultilineTextbox ||
                productAttributeMapping.AttributeControlTypeId == AttributeControlType.Datepicker ||
                productAttributeMapping.AttributeControlTypeId == AttributeControlType.FileUpload)
                return false;

            return true;
        }

        public static bool ValidationRulesAllowed(this ProductAttributeMapping productAttributeMapping)
        {
            if (productAttributeMapping == null)
                return false;

            if (productAttributeMapping.AttributeControlTypeId == AttributeControlType.TextBox ||
                productAttributeMapping.AttributeControlTypeId == AttributeControlType.MultilineTextbox ||
                productAttributeMapping.AttributeControlTypeId == AttributeControlType.FileUpload)
                return true;

            return false;
        }

        public static bool CanBeUsedAsCondition(this ProductAttributeMapping productAttributeMapping)
        {
            if (productAttributeMapping == null)
                return false;

            if (productAttributeMapping.AttributeControlTypeId == AttributeControlType.ReadonlyCheckboxes ||
                productAttributeMapping.AttributeControlTypeId == AttributeControlType.TextBox ||
                productAttributeMapping.AttributeControlTypeId == AttributeControlType.MultilineTextbox ||
                productAttributeMapping.AttributeControlTypeId == AttributeControlType.Datepicker ||
                productAttributeMapping.AttributeControlTypeId == AttributeControlType.FileUpload)
                return false;

            return true;
        }
       
        public static bool IsNonCombinable(this ProductAttributeMapping productAttributeMapping)
        {           
            if (productAttributeMapping == null)
                return false;

            if (!productAttributeMapping.Combination)
                return true;

            var result = !ShouldHaveValues(productAttributeMapping);
            return result;
        }
    }
}
