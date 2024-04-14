using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Courses;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Courses;
using Grand.Domain.Seo;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Courses;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class CourseViewModelService(
    ICourseService courseService,
    ICourseLevelService courseLevelService,
    ICourseLessonService courseLessonService,
    ICourseSubjectService courseSubjectService,
    ISlugService slugService,
    IPictureService pictureService,
    ILanguageService languageService,
    ITranslationService translationService,
    IProductCourseService productCourseService,
    IDownloadService downloadService,
    IProductService productService,
    IStoreService storeService,
    IVendorService vendorService,
    SeoSettings seoSettings)
    : ICourseViewModelService
{
    public virtual async Task<CourseModel> PrepareCourseModel(CourseModel model = null)
    {
        model ??= new CourseModel {
            Published = true
        };

        foreach (var item in await courseLevelService.GetAll())
            model.AvailableLevels.Add(new SelectListItem {
                Text = item.Name,
                Value = item.Id
            });
        if (!string.IsNullOrEmpty(model.ProductId))
            model.ProductName = (await productService.GetProductById(model.ProductId))?.Name;
        return model;
    }

    public virtual async Task<Course> InsertCourseModel(CourseModel model)
    {
        var course = model.ToEntity();
        await courseService.Insert(course);

        //locales
        model.SeName =
            await course.ValidateSeName(model.SeName, course.Name, true, seoSettings, slugService, languageService);
        course.SeName = model.SeName;
        await courseService.Update(course);

        await slugService.SaveSlug(course, model.SeName, "");

        //update picture seo file name
        await pictureService.UpdatePictureSeoNames(course.PictureId, course.Name);

        //course on the product 
        if (!string.IsNullOrEmpty(course.ProductId))
            await productCourseService.UpdateCourseOnProduct(course.ProductId, course.Id);

        return course;
    }

    public virtual async Task<Course> UpdateCourseModel(Course course, CourseModel model)
    {
        var prevPictureId = course.PictureId;
        var prevProductId = course.ProductId;

        course = model.ToEntity(course);
        model.SeName =
            await course.ValidateSeName(model.SeName, course.Name, true, seoSettings, slugService, languageService);
        course.SeName = model.SeName;
        //locales
        course.Locales =
            await model.Locales.ToTranslationProperty(course, x => x.Name, seoSettings, slugService, languageService);
        await courseService.Update(course);
        //search engine name
        await slugService.SaveSlug(course, model.SeName, "");

        //delete an old picture (if deleted or updated)
        if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != course.PictureId)
        {
            var prevPicture = await pictureService.GetPictureById(prevPictureId);
            if (prevPicture != null)
                await pictureService.DeletePicture(prevPicture);
        }

        //update picture seo file name
        await pictureService.UpdatePictureSeoNames(course.PictureId, course.Name);

        //course on the product 
        if (!string.IsNullOrEmpty(prevProductId))
            await productCourseService.UpdateCourseOnProduct(prevProductId, string.Empty);

        if (!string.IsNullOrEmpty(course.ProductId))
            await productCourseService.UpdateCourseOnProduct(course.ProductId, course.Id);

        return course;
    }

    public virtual async Task DeleteCourse(Course course)
    {
        await courseService.Delete(course);
    }

    public virtual async Task<CourseLessonModel> PrepareCourseLessonModel(string courseId,
        CourseLessonModel model = null)
    {
        model ??= new CourseLessonModel {
            Published = true
        };
        model.CourseId = courseId;

        foreach (var item in await courseSubjectService.GetByCourseId(courseId))
            model.AvailableSubjects.Add(new SelectListItem {
                Text = item.Name,
                Value = item.Id
            });

        return model;
    }

    public virtual async Task<CourseLesson> InsertCourseLessonModel(CourseLessonModel model)
    {
        var lesson = model.ToEntity();
        await courseLessonService.Insert(lesson);

        return lesson;
    }

    public virtual async Task<CourseLesson> UpdateCourseLessonModel(CourseLesson lesson, CourseLessonModel model)
    {
        var prevAttachmentId = lesson.AttachmentId;
        var prevVideoFile = lesson.VideoFile;

        var prevPictureId = lesson.PictureId;
        lesson = model.ToEntity(lesson);
        await courseLessonService.Update(lesson);

        //delete an old picture (if deleted or updated)
        if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != lesson.PictureId)
        {
            var prevPicture = await pictureService.GetPictureById(prevPictureId);
            if (prevPicture != null)
                await pictureService.DeletePicture(prevPicture);
        }

        //delete an old "attachment" file (if deleted or updated)
        if (!string.IsNullOrEmpty(prevAttachmentId) && prevAttachmentId != lesson.AttachmentId)
        {
            var prevAttachment = await downloadService.GetDownloadById(prevAttachmentId);
            if (prevAttachment != null)
                await downloadService.DeleteDownload(prevAttachment);
        }

        //delete an old "video" file (if deleted or updated)
        if (!string.IsNullOrEmpty(prevVideoFile) && prevVideoFile != lesson.VideoFile)
        {
            var prevVideo = await downloadService.GetDownloadById(prevVideoFile);
            if (prevVideo != null)
                await downloadService.DeleteDownload(prevVideo);
        }

        return lesson;
    }

    public virtual async Task DeleteCourseLesson(CourseLesson lesson)
    {
        await courseLessonService.Delete(lesson);

        if (!string.IsNullOrEmpty(lesson.VideoFile))
        {
            var prevVideo = await downloadService.GetDownloadById(lesson.VideoFile);
            if (prevVideo != null)
                await downloadService.DeleteDownload(prevVideo);
        }

        if (!string.IsNullOrEmpty(lesson.AttachmentId))
        {
            var prevAttachment = await downloadService.GetDownloadById(lesson.AttachmentId);
            if (prevAttachment != null)
                await downloadService.DeleteDownload(prevAttachment);
        }
    }

    public virtual async Task<CourseModel.AssociateProductToCourseModel> PrepareAssociateProductToCourseModel()
    {
        var model = new CourseModel.AssociateProductToCourseModel();
        //stores
        model.AvailableStores.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var s in await storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

        //vendors
        model.AvailableVendors.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var v in await vendorService.GetAllVendors(showHidden: true))
            model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id });

        //product types
        model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList().ToList();
        model.AvailableProductTypes.Insert(0,
            new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = " " });
        return model;
    }

    public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(
        CourseModel.AssociateProductToCourseModel model, int pageIndex, int pageSize)
    {
        var products = await productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId,
            model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId,
            model.SearchProductName, pageIndex, pageSize);
        return (products.Select(x => x.ToModel()).ToList(), products.TotalCount);
    }
}