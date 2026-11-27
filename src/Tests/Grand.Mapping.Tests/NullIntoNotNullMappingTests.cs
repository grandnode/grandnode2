using System.Reflection;
using Grand.Mapping.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Mapping.Tests;

/// <summary>
///     The mapper copies a null string as it is, and the compiler cannot see it do so: a profile that maps a
///     nullable model or DTO member onto a Domain member declared not null stores null unless something
///     upstream refuses it. Every such member is listed here with what refuses it.
/// </summary>
[TestClass]
public class NullIntoNotNullMappingTests
{
    private const string Validator = "every validator of the source requires it";
    private const string ImportCondition = "mapped only when given; the import requires it for a new record";

    private static readonly Dictionary<string, string> Guarded = new() {
        ["Grand.Business.Core.Dto.BrandDto -> Brand.Name"] = ImportCondition,
        ["Grand.Business.Core.Dto.CategoryDto -> Category.Name"] = ImportCondition,
        ["Grand.Business.Core.Dto.CollectionDto -> Collection.Name"] = ImportCondition,
        ["Grand.Business.Core.Dto.ProductDto -> Product.Name"] = ImportCondition,
        ["Grand.Module.Api.DTOs.Catalog.BrandDto -> Brand.Name"] = Validator,
        ["Grand.Module.Api.DTOs.Catalog.CategoryDto -> Category.Name"] = Validator,
        ["Grand.Module.Api.DTOs.Catalog.CollectionDto -> Collection.Name"] = Validator,
        ["Grand.Module.Api.DTOs.Catalog.PredefinedProductAttributeValueDto -> PredefinedProductAttributeValue.Name"] = "ProductAttributeValidator requires the name of every predefined value",
        ["Grand.Module.Api.DTOs.Catalog.ProductAttributeDto -> ProductAttribute.Name"] = Validator,
        ["Grand.Module.Api.DTOs.Catalog.ProductAttributeMappingDto -> ProductAttributeMapping.ProductAttributeId"] = "ProductAttributeMappingValidator requires an existing attribute",
        ["Grand.Module.Api.DTOs.Catalog.ProductDto -> Product.Name"] = Validator,
        ["Grand.Module.Api.DTOs.Catalog.SpecificationAttributeDto -> SpecificationAttribute.Name"] = Validator,
        ["Grand.Module.Api.DTOs.Catalog.SpecificationAttributeOptionDto -> SpecificationAttributeOption.Name"] = "SpecificationAttributeValidator requires the name of every option",
        ["Grand.Module.Api.DTOs.Customers.CustomerGroupDto -> CustomerGroup.Name"] = Validator,
        ["Grand.Module.Api.DTOs.Orders.MerchandiseReturnActionDto -> MerchandiseReturnAction.Name"] = Validator,
        ["Grand.Module.Api.DTOs.Orders.MerchandiseReturnReasonDto -> MerchandiseReturnReason.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Blogs.BlogCategoryModel -> BlogCategory.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Blogs.BlogPostModel -> BlogPost.Body"] = Validator,
        ["Grand.Web.AdminShared.Models.Blogs.BlogPostModel -> BlogPost.Title"] = Validator,
        ["Grand.Web.AdminShared.Models.Catalog.BrandModel -> Brand.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Catalog.CategoryModel -> Category.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Catalog.CollectionModel -> Collection.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Catalog.PredefinedProductAttributeValueModel -> PredefinedProductAttributeValue.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Catalog.ProductAttributeModel -> ProductAttribute.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Catalog.ProductModel -> Product.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Catalog.ProductModel+ProductAttributeMappingModel -> ProductAttributeMapping.ProductAttributeId"] = "ProductAttributeMappingViewModelService refuses a mapping without an attribute",
        ["Grand.Web.AdminShared.Models.Catalog.ProductReviewModel -> ProductReview.ReviewText"] = Validator,
        ["Grand.Web.AdminShared.Models.Catalog.ProductReviewModel -> ProductReview.Title"] = Validator,
        ["Grand.Web.AdminShared.Models.Catalog.SpecificationAttributeModel -> SpecificationAttribute.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Catalog.SpecificationAttributeOptionModel -> SpecificationAttributeOption.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Common.AddressAttributeModel -> AddressAttribute.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Common.AddressAttributeValueModel -> AddressAttributeValue.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Courses.CourseLessonModel -> CourseLesson.CourseId"] = Validator,
        ["Grand.Web.AdminShared.Models.Courses.CourseLessonModel -> CourseLesson.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Courses.CourseLessonModel -> CourseLesson.SubjectId"] = Validator,
        ["Grand.Web.AdminShared.Models.Courses.CourseLevelModel -> CourseLevel.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Courses.CourseModel -> Course.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Courses.CourseSubjectModel -> CourseSubject.CourseId"] = Validator,
        ["Grand.Web.AdminShared.Models.Courses.CourseSubjectModel -> CourseSubject.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Customers.CustomerAttributeModel -> CustomerAttribute.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Customers.CustomerAttributeValueModel -> CustomerAttributeValue.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Customers.CustomerGroupModel -> CustomerGroup.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Customers.CustomerTagModel -> CustomerTag.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Customers.SalesEmployeeModel -> SalesEmployee.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Customers.UserApiCreateModel -> UserApi.Email"] = Validator,
        ["Grand.Web.AdminShared.Models.Customers.UserApiModel -> UserApi.Email"] = Validator,
        ["Grand.Web.AdminShared.Models.Directory.CountryModel -> Country.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Directory.CountryModel -> Country.ThreeLetterIsoCode"] = Validator,
        ["Grand.Web.AdminShared.Models.Directory.CountryModel -> Country.TwoLetterIsoCode"] = Validator,
        ["Grand.Web.AdminShared.Models.Directory.CurrencyModel -> Currency.CurrencyCode"] = Validator,
        ["Grand.Web.AdminShared.Models.Directory.CurrencyModel -> Currency.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Directory.MeasureDimensionModel -> MeasureDimension.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Directory.MeasureDimensionModel -> MeasureDimension.SystemKeyword"] = Validator,
        ["Grand.Web.AdminShared.Models.Directory.MeasureUnitModel -> MeasureUnit.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Directory.MeasureWeightModel -> MeasureWeight.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Directory.MeasureWeightModel -> MeasureWeight.SystemKeyword"] = Validator,
        ["Grand.Web.AdminShared.Models.Directory.StateProvinceModel -> StateProvince.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Discounts.DiscountModel -> Discount.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Documents.DocumentModel -> Document.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Documents.DocumentModel -> Document.Number"] = Validator,
        ["Grand.Web.AdminShared.Models.Documents.DocumentTypeModel -> DocumentType.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Knowledgebase.KnowledgebaseArticleModel -> KnowledgebaseArticle.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Knowledgebase.KnowledgebaseCategoryModel -> KnowledgebaseCategory.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Layouts.BrandLayoutModel -> BrandLayout.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Layouts.BrandLayoutModel -> BrandLayout.ViewPath"] = Validator,
        ["Grand.Web.AdminShared.Models.Layouts.CategoryLayoutModel -> CategoryLayout.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Layouts.CategoryLayoutModel -> CategoryLayout.ViewPath"] = Validator,
        ["Grand.Web.AdminShared.Models.Layouts.CollectionLayoutModel -> CollectionLayout.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Layouts.CollectionLayoutModel -> CollectionLayout.ViewPath"] = Validator,
        ["Grand.Web.AdminShared.Models.Layouts.PageLayoutModel -> PageLayout.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Layouts.PageLayoutModel -> PageLayout.ViewPath"] = Validator,
        ["Grand.Web.AdminShared.Models.Layouts.ProductLayoutModel -> ProductLayout.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Layouts.ProductLayoutModel -> ProductLayout.ViewPath"] = Validator,
        ["Grand.Web.AdminShared.Models.Localization.LanguageModel -> Language.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Localization.LanguageModel -> Language.UniqueSeoCode"] = Validator,
        ["Grand.Web.AdminShared.Models.Messages.CampaignModel -> Campaign.Body"] = Validator,
        ["Grand.Web.AdminShared.Models.Messages.CampaignModel -> Campaign.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Messages.CampaignModel -> Campaign.Subject"] = Validator,
        ["Grand.Web.AdminShared.Models.Messages.ContactAttributeModel -> ContactAttribute.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Messages.ContactAttributeValueModel -> ContactAttributeValue.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Messages.EmailAccountModel -> EmailAccount.DisplayName"] = Validator,
        ["Grand.Web.AdminShared.Models.Messages.EmailAccountModel -> EmailAccount.Email"] = Validator,
        ["Grand.Web.AdminShared.Models.Messages.MessageTemplateModel -> MessageTemplate.Body"] = Validator,
        ["Grand.Web.AdminShared.Models.Messages.MessageTemplateModel -> MessageTemplate.Subject"] = Validator,
        ["Grand.Web.AdminShared.Models.Messages.NewsletterCategoryModel -> NewsletterCategory.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Messages.NewsLetterSubscriptionModel -> NewsLetterSubscription.Email"] = Validator,
        ["Grand.Web.AdminShared.Models.News.NewsItemModel -> NewsItem.Full"] = Validator,
        ["Grand.Web.AdminShared.Models.News.NewsItemModel -> NewsItem.Short"] = Validator,
        ["Grand.Web.AdminShared.Models.News.NewsItemModel -> NewsItem.Title"] = Validator,
        ["Grand.Web.AdminShared.Models.Orders.CheckoutAttributeModel -> CheckoutAttribute.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Orders.CheckoutAttributeValueModel -> CheckoutAttributeValue.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Orders.GiftVoucherModel -> GiftVoucher.Code"] = Validator,
        ["Grand.Web.AdminShared.Models.Orders.OrderStatusModel -> OrderStatus.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Settings.MerchandiseReturnActionModel -> MerchandiseReturnAction.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Settings.MerchandiseReturnReasonModel -> MerchandiseReturnReason.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Shipping.DeliveryDateModel -> DeliveryDate.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Shipping.PickupPointModel -> PickupPoint.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Shipping.ShippingMethodModel -> ShippingMethod.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Shipping.WarehouseModel -> Warehouse.Code"] = Validator,
        ["Grand.Web.AdminShared.Models.Shipping.WarehouseModel -> Warehouse.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Stores.DomainHostModel -> DomainHost.Url"] = Validator,
        ["Grand.Web.AdminShared.Models.Stores.StoreModel -> Store.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Stores.StoreModel -> Store.Url"] = Validator,
        ["Grand.Web.AdminShared.Models.Tax.TaxCategoryModel -> TaxCategory.Name"] = Validator,
        ["Grand.Web.AdminShared.Models.Vendors.VendorModel -> Vendor.Email"] = Validator,
        ["Grand.Web.AdminShared.Models.Vendors.VendorModel -> Vendor.Name"] = Validator,
        ["Grand.Web.Vendor.Models.Vendor.VendorModel -> Vendor.Email"] = Validator,
        ["Grand.Web.Vendor.Models.Vendor.VendorModel -> Vendor.Name"] = Validator,
    };

    private static readonly Assembly[] ProfileAssemblies = [
        typeof(Web.AdminShared.Mapper.CategoryProfile).Assembly,
        typeof(Module.Api.Infrastructure.Mapper.Profiles.ProductAttributeMappingProfile).Assembly,
        typeof(Business.Catalog.Services.ExportImport.Mapper.BrandProfile).Assembly,
        typeof(Web.Vendor.Mapper.VendorProfile).Assembly
    ];

    [TestMethod]
    public void EveryNullableSourceOfANotNullDomainMemberIsGuarded()
    {
        var unguarded = NullableIntoNotNull().Where(x => !Guarded.ContainsKey(x)).Order().ToList();

        Assert.AreEqual(0, unguarded.Count,
            "These mappings can store null in a member declared not null. Require the member in the source's " +
            "validator and list it here, ignore it or map it with MapFrom, or declare the Domain member nullable:" +
            Environment.NewLine + string.Join(Environment.NewLine, unguarded));
    }

    [TestMethod]
    public void EveryGuardedMappingStillExists()
    {
        var found = NullableIntoNotNull().ToHashSet();
        var stale = Guarded.Keys.Where(x => !found.Contains(x)).Order().ToList();

        Assert.AreEqual(0, stale.Count,
            "These entries no longer match a mapping of a nullable source onto a not-null member; remove them:" +
            Environment.NewLine + string.Join(Environment.NewLine, stale));
    }

    /// <summary>
    ///     The Domain members a profile assigns straight from a nullable source member of the same name, as
    ///     MappingCompiler does: an ignored member, MapFrom, a collection (null becomes empty) and a nested
    ///     mapping (null leaves the destination as it is) are left out
    /// </summary>
    private static IEnumerable<string> NullableIntoNotNull()
    {
        var nullability = new NullabilityInfoContext();
        foreach (var profile in ProfileAssemblies.SelectMany(x => x.GetTypes())
                     .Where(x => !x.IsAbstract && typeof(Profile).IsAssignableFrom(x))
                     .Select(x => (Profile)Activator.CreateInstance(x)!))
        foreach (var configuration in profile.GetConfigurations())
        {
            var (source, dest) = configuration.GetTypes();
            if (dest.Namespace?.StartsWith("Grand.Domain") != true) continue;

            var configured = configuration.MemberConfigs.Where(x => !x.IsPath)
                .ToDictionary(x => x.MemberName, StringComparer.Ordinal);
            foreach (var member in dest.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(x => x.CanWrite && !x.PropertyType.IsValueType))
            {
                if (configured.TryGetValue(member.Name, out var config) &&
                    (config.IsIgnored || config.MapFromExpression != null)) continue;
                if (nullability.Create(member).WriteState != NullabilityState.NotNull) continue;

                var sourceMember = source.GetProperty(member.Name, BindingFlags.Public | BindingFlags.Instance);
                if (sourceMember is not { CanRead: true } ||
                    !member.PropertyType.IsAssignableFrom(sourceMember.PropertyType) ||
                    IsCollection(member.PropertyType)) continue;
                if (nullability.Create(sourceMember).ReadState == NullabilityState.NotNull) continue;

                yield return $"{source.FullName} -> {dest.Name}.{member.Name}";
            }
        }
    }

    private static bool IsCollection(Type type)
        => type != typeof(string) && type.GetInterfaces().Append(type)
            .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
}
