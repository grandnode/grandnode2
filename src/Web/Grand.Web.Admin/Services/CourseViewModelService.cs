using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Courses;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Courses;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Courses;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class CourseViewModelService : ICourseViewModelService
    {
        private readonly ICourseService _courseService;
        private readonly ICourseLevelService _courseLevelService;
        private readonly ICourseLessonService _courseLessonService;
        private readonly ICourseSubjectService _courseSubjectService;
        private readonly ISlugService _slugService;
        private readonly IPictureService _pictureService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IProductCourseService _productCourseService;
        private readonly IServiceProvider _serviceProvider;
        private readonly SeoSettings _seoSettings;

        public CourseViewModelService(ICourseService courseService, ICourseLevelService courseLevelService, ICourseLessonService courseLessonService,
            ICourseSubjectService courseSubjectService,
            ISlugService slugService, IPictureService pictureService, ILanguageService languageService,
            ITranslationService translationService, ICustomerActivityService customerActivityService, IProductCourseService productCourseService,
            IServiceProvider serviceProvider,
            SeoSettings seoSettings)
        {
            _courseService = courseService;
            _courseLevelService = courseLevelService;
            _courseLessonService = courseLessonService;
            _courseSubjectService = courseSubjectService;
            _slugService = slugService;
            _pictureService = pictureService;
            _languageService = languageService;
            _translationService = translationService;
            _customerActivityService = customerActivityService;
            _productCourseService = productCourseService;
            _serviceProvider = serviceProvider;
            _seoSettings = seoSettings;
        }

        public virtual async Task<CourseModel> PrepareCourseModel(CourseModel model = null)
        {
            if (model == null)
            {
                model = new CourseModel();
                model.Published = true;
            }

            foreach (var item in await _courseLevelService.GetAll())
            {
                model.AvailableLevels.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Text = item.Name,
                    Value = item.Id
                });
            }
            if (!string.IsNullOrEmpty(model.ProductId))
            {
                var productService = _serviceProvider.GetRequiredService<IProductService>();
                model.ProductName = (await productService.GetProductById(model.ProductId))?.Name;
            }
            return model;
        }

        public virtual async Task<Course> InsertCourseModel(CourseModel model)
        {
            var course = model.ToEntity();
            course.CreatedOnUtc = DateTime.UtcNow;
            course.UpdatedOnUtc = DateTime.UtcNow;

            await _courseService.Insert(course);

            //locales
            model.SeName = await course.ValidateSeName(model.SeName, course.Name, true, _seoSettings, _slugService, _languageService);
            course.SeName = model.SeName;
            await _courseService.Update(course);

            await _slugService.SaveSlug(course, model.SeName, "");

            //update picture seo file name
            await _pictureService.UpdatePictureSeoNames(course.PictureId, course.Name);

            //course on the product 
            if (!string.IsNullOrEmpty(course.ProductId))
                await _productCourseService.UpdateCourseOnProduct(course.ProductId, course.Id);


            //activity log
            await _customerActivityService.InsertActivity("AddNewCourse", course.Id, _translationService.GetResource("ActivityLog.AddNewCourse"), course.Name);

            return course;
        }

        public virtual async Task<Course> UpdateCourseModel(Course course, CourseModel model)
        {
            string prevPictureId = course.PictureId;
            string prevProductId = course.ProductId;

            course = model.ToEntity(course);
            course.UpdatedOnUtc = DateTime.UtcNow;
            model.SeName = await course.ValidateSeName(model.SeName, course.Name, true, _seoSettings, _slugService, _languageService);
            course.SeName = model.SeName;
            //locales
            course.Locales = await model.Locales.ToTranslationProperty(course, x => x.Name, _seoSettings, _slugService, _languageService);
            await _courseService.Update(course);
            //search engine name
            await _slugService.SaveSlug(course, model.SeName, "");

            //delete an old picture (if deleted or updated)
            if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != course.PictureId)
            {
                var prevPicture = await _pictureService.GetPictureById(prevPictureId);
                if (prevPicture != null)
                    await _pictureService.DeletePicture(prevPicture);
            }
            //update picture seo file name
            await _pictureService.UpdatePictureSeoNames(course.PictureId, course.Name);

            //course on the product 
            if (!string.IsNullOrEmpty(prevProductId))
                await _productCourseService.UpdateCourseOnProduct(prevProductId, string.Empty);

            if (!string.IsNullOrEmpty(course.ProductId))
                await _productCourseService.UpdateCourseOnProduct(course.ProductId, course.Id);


            //activity log
            await _customerActivityService.InsertActivity("EditCourse", course.Id, _translationService.GetResource("ActivityLog.EditCourse"), course.Name);

            return course;
        }
        public virtual async Task DeleteCourse(Course course)
        {
            await _courseService.Delete(course);
            //activity log
            await _customerActivityService.InsertActivity("DeleteCourse", course.Id, _translationService.GetResource("ActivityLog.DeleteCourse"), course.Name);
        }

        public virtual async Task<CourseLessonModel> PrepareCourseLessonModel(string courseId, CourseLessonModel model = null)
        {
            if (model == null)
            {
                model = new CourseLessonModel();
                model.Published = true;
            }
            model.CourseId = courseId;

            foreach (var item in await _courseSubjectService.GetByCourseId(courseId))
            {
                model.AvailableSubjects.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Text = item.Name,
                    Value = item.Id
                });
            }

            return model;
        }

        public virtual async Task<CourseLesson> InsertCourseLessonModel(CourseLessonModel model)
        {
            var lesson = model.ToEntity();
            await _courseLessonService.Insert(lesson);
            //activity log
            await _customerActivityService.InsertActivity("AddNewCourseLesson", lesson.Id, _translationService.GetResource("ActivityLog.AddNewCourseLesson"), lesson.Name);
            return lesson;
        }

        public virtual async Task<CourseLesson> UpdateCourseLessonModel(CourseLesson lesson, CourseLessonModel model)
        {
            string prevPictureId = lesson.PictureId;
            lesson = model.ToEntity(lesson);
            await _courseLessonService.Update(lesson);

            //delete an old picture (if deleted or updated)
            if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != lesson.PictureId)
            {
                var prevPicture = await _pictureService.GetPictureById(prevPictureId);
                if (prevPicture != null)
                    await _pictureService.DeletePicture(prevPicture);
            }

            //activity log
            await _customerActivityService.InsertActivity("EditCourseLesson", lesson.Id, _translationService.GetResource("ActivityLog.EditLessonCourse"), lesson.Name);

            return lesson;
        }
        public virtual async Task DeleteCourseLesson(CourseLesson lesson)
        {
            await _courseLessonService.Delete(lesson);
            //activity log
            await _customerActivityService.InsertActivity("DeleteCourseLesson", lesson.Id, _translationService.GetResource("ActivityLog.DeleteCourseLesson"), lesson.Name);
        }

        public virtual async Task<CourseModel.AssociateProductToCourseModel> PrepareAssociateProductToCourseModel()
        {
            var model = new CourseModel.AssociateProductToCourseModel();
            //a vendor should have access only to his products
            var workContext = _serviceProvider.GetRequiredService<IWorkContext>();
            model.IsLoggedInAsVendor = workContext.CurrentVendor != null;

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            var storeService = _serviceProvider.GetRequiredService<IStoreService>();
            foreach (var s in await storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            //vendors
            model.AvailableVendors.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            var vendorService = _serviceProvider.GetRequiredService<IVendorService>();
            foreach (var v in await vendorService.GetAllVendors(showHidden: true))
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });

            //product types
            model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList().ToList();
            model.AvailableProductTypes.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            return model;
        }
        public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(CourseModel.AssociateProductToCourseModel model, int pageIndex, int pageSize)
        {
            var productService = _serviceProvider.GetRequiredService<IProductService>();
            var dateTimeService = _serviceProvider.GetRequiredService<IDateTimeService>();
            var products = await productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId, model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId, model.SearchProductName, pageIndex, pageSize);
            return (products.Select(x => x.ToModel(dateTimeService)).ToList(), products.TotalCount);
        }
    }
}
