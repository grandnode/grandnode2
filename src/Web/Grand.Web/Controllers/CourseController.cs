using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Marketing.Interfaces.Courses;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Courses;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Courses;
using Grand.Web.Features.Models.Courses;
using MediatR;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Threading.Tasks;

namespace Grand.Web.Controllers
{
    public partial class CourseController : BasePublicController
    {
        private readonly IPermissionService _permissionService;
        private readonly IAclService _aclService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IUserFieldService _userFieldService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerActionEventService _customerActionEventService;
        private readonly ICourseService _courseService;
        private readonly ICourseLessonService _courseLessonService;
        private readonly IDownloadService _downloadService;
        private readonly IMediator _mediator;
        private readonly CourseSettings _courseSettings;

        public CourseController(
            IPermissionService permissionService,
            IAclService aclService,
            IWorkContext workContext,
            IGroupService groupService,
            ICustomerActivityService customerActivityService,
            IUserFieldService userFieldService,
            ITranslationService translationService,
            ICustomerActionEventService customerActionEventService,
            ICourseService courseService,
            ICourseLessonService courseLessonService,
            IDownloadService downloadService,
            IMediator mediator,
            CourseSettings courseSettings)
        {
            _permissionService = permissionService;
            _aclService = aclService;
            _workContext = workContext;
            _groupService = groupService;
            _customerActivityService = customerActivityService;
            _userFieldService = userFieldService;
            _translationService = translationService;
            _customerActionEventService = customerActionEventService;
            _courseService = courseService;
            _courseLessonService = courseLessonService;
            _downloadService = downloadService;
            _mediator = mediator;
            _courseSettings = courseSettings;
        }

        protected async Task<bool> CheckPermission(Course course, Customer customer)
        {
            //Check whether the current user is a guest
            if (await _groupService.IsGuest(customer) && !_courseSettings.AllowGuestsToAccessCourse)
            {
                return false;
            }

            //Check whether the current user has a "Manage course" permission
            //It allows him to preview a category before publishing
            if (!course.Published && !await _permissionService.Authorize(StandardPermission.ManageCourses, customer))
                return false;

            //Check whether the current user purchased the course
            if (!await _mediator.Send(new GetCheckOrder() { Course = course, Customer = customer })
                && !await _permissionService.Authorize(StandardPermission.ManageCourses, customer))
                return false;

            //ACL (access control list)
            if (!_aclService.Authorize(course, customer))
                return false;

            //Store access
            if (!_aclService.Authorize(course, _workContext.CurrentStore.Id))
                return false;

            return true;
        }

        public virtual async Task<IActionResult> Details(string courseId)
        {
            var customer = _workContext.CurrentCustomer;

            var course = await _courseService.GetById(courseId);
            if (course == null)
                return InvokeHttp404();

            if (!await CheckPermission(course, customer))
                return InvokeHttp404();

            //'Continue shopping' URL
            await _userFieldService.SaveField(customer, SystemCustomerFieldNames.LastContinueShoppingPage, HttpContext?.Request?.GetDisplayUrl(), _workContext.CurrentStore.Id);

            //display "edit" (manage) link
            if (await _permissionService.Authorize(StandardPermission.AccessAdminPanel, customer) && await _permissionService.Authorize(StandardPermission.ManageCourses, customer))
                DisplayEditLink(Url.Action("Edit", "Course", new { id = course.Id, area = "Admin" }));

            //activity log
            await _customerActivityService.InsertActivity("PublicStore.ViewCourse", course.Id, _translationService.GetResource("ActivityLog.PublicStore.ViewCourse"), course.Name);
            await _customerActionEventService.Viewed(customer, HttpContext.Request.Path.ToString(), Request.Headers[HeaderNames.Referer].ToString() != null ? Request.Headers["Referer"].ToString() : "");

            //model
            var model = await _mediator.Send(new GetCourse()
            {
                Course = course,
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage
            });

            return View(model);
        }
        public virtual async Task<IActionResult> Lesson(string id)
        {
            var customer = _workContext.CurrentCustomer;

            var lesson = await _courseLessonService.GetById(id);
            if (lesson == null)
                return InvokeHttp404();

            var course = await _courseService.GetById(lesson.CourseId);
            if (course == null)
                return InvokeHttp404();

            if (!await CheckPermission(course, customer))
                return InvokeHttp404();

            //'Continue shopping' URL
            await _userFieldService.SaveField(customer, SystemCustomerFieldNames.LastContinueShoppingPage, HttpContext?.Request?.GetDisplayUrl(), _workContext.CurrentStore.Id);

            //display "edit" (manage) link
            if (await _permissionService.Authorize(StandardPermission.AccessAdminPanel, customer) && await _permissionService.Authorize(StandardPermission.ManageCourses, customer))
                DisplayEditLink(Url.Action("EditLesson", "Course", new { id = lesson.Id, area = "Admin" }));

            //activity log
            await _customerActivityService.InsertActivity("PublicStore.ViewLesson", lesson.Id, _translationService.GetResource("ActivityLog.PublicStore.ViewLesson"), lesson.Name);
            await _customerActionEventService.Viewed(customer, HttpContext.Request.Path.ToString(), Request.Headers[HeaderNames.Referer].ToString() != null ? Request.Headers["Referer"].ToString() : "");

            //model
            var model = await _mediator.Send(new GetLesson()
            {
                Course = course,
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Lesson = lesson
            });

            return View(model);
        }
        public virtual async Task<IActionResult> DownloadFile(string id)
        {
            var customer = _workContext.CurrentCustomer;

            var lesson = await _courseLessonService.GetById(id);
            if (lesson == null || string.IsNullOrEmpty(lesson.AttachmentId))
                return InvokeHttp404();

            var course = await _courseService.GetById(lesson.CourseId);
            if (course == null)
                return InvokeHttp404();

            if (!await CheckPermission(course, customer))
                return InvokeHttp404();

            var download = await _downloadService.GetDownloadById(lesson.AttachmentId);
            if (download == null)
                return Content("No download record found with the specified id");

            if (download.UseDownloadUrl)
                return new RedirectResult(download.DownloadUrl);

            //use stored data
            if (download.DownloadBinary == null)
                return Content(string.Format("Download data is not available any more. Download GD={0}", download.Id));

            string fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : download.Id.ToString();
            string contentType = !string.IsNullOrWhiteSpace(download.ContentType)
                ? download.ContentType
                : "application/octet-stream";
            return new FileContentResult(download.DownloadBinary, contentType)
            {
                FileDownloadName = fileName + download.Extension
            };
        }

        public virtual async Task<IActionResult> VideoFile(string id)
        {
            var customer = _workContext.CurrentCustomer;

            var lesson = await _courseLessonService.GetById(id);
            if (lesson == null || string.IsNullOrEmpty(lesson.VideoFile))
                return InvokeHttp404();

            var course = await _courseService.GetById(lesson.CourseId);
            if (course == null)
                return InvokeHttp404();

            if (!await CheckPermission(course, customer))
                return InvokeHttp404();

            var download = await _downloadService.GetDownloadById(lesson.VideoFile);
            if (download == null)
                return Content("No download record found with the specified id");

            if (download.UseDownloadUrl)
                return new RedirectResult(download.DownloadUrl);

            //use stored data
            if (download.DownloadBinary == null)
                return Content(string.Format("Download data is not available any more. Download GD={0}", download.Id));

            string fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : download.Id.ToString();
            string contentType = !string.IsNullOrWhiteSpace(download.ContentType)
                ? download.ContentType
                : "video/mp4";
            return new FileContentResult(download.DownloadBinary, contentType)
            {
                FileDownloadName = fileName + download.Extension
            };
        }

        public virtual async Task<IActionResult> Approved(string id)
        {
            var customer = _workContext.CurrentCustomer;

            var lesson = await _courseLessonService.GetById(id);
            if (lesson == null)
                return Json(new { result = false });

            var course = await _courseService.GetById(lesson.CourseId);
            if (course == null)
                return Json(new { result = false });

            if (!await CheckPermission(course, customer))
                return Json(new { result = false });

            await _mediator.Send(new CourseLessonApprovedCommand() { Course = course, Lesson = lesson, Customer = _workContext.CurrentCustomer });

            return Json(new { result = true });
        }
    }
}
