using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Reflection;

namespace Grand.Web.Common.Localization;

public interface IEnumTranslationService
{
    SelectList ToSelectList<TEnum>(TEnum enumObj, bool markCurrentAsSelected = true, int[] valuesToExclude = null) where TEnum : struct;
    string GetTranslationEnum<T>(T enumValue) where T : struct;
}

public class EnumTranslationService(ITranslationService translationService, IContextAccessor contextAccessor) : IEnumTranslationService
{
    public SelectList ToSelectList<TEnum>(TEnum enumObj, bool markCurrentAsSelected = true, int[] valuesToExclude = null)
        where TEnum : struct
    {
        if (!typeof(TEnum).GetTypeInfo().IsEnum)
            throw new ArgumentException("Enumeration type is required.");

        var values = from TEnum enumValue in Enum.GetValues(typeof(TEnum))
            where valuesToExclude == null || !valuesToExclude.Contains(Convert.ToInt32(enumValue))
            select new
            {
                ID = Convert.ToInt32(enumValue),
                Name = GetTranslationEnum(enumValue)
            };

        object selectedValue = null;
        if (markCurrentAsSelected)
            selectedValue = Convert.ToInt32(enumObj);

        return new SelectList(values, "ID", "Name", selectedValue);
    }
    
    public string GetTranslationEnum<T>(T enumValue) where T : struct
    {
        if (!typeof(T).GetTypeInfo().IsEnum) throw new ArgumentException("T must be enum type");

        //Translation value
        var resourceName = $"Enums.{typeof(T)}.{enumValue.ToString()}";
        var result = translationService.GetResource(resourceName, contextAccessor.WorkContext.WorkingLanguage.Id, "", true);

        //set default value if required
        if (string.IsNullOrEmpty(result))
            result = CommonHelper.ConvertEnum(enumValue);

        return result;
    }
}