﻿using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers
{
    [AllowAnonymous]
    public class LetsEncryptController : Controller
    {
        private readonly CommonSettings _commonSettings;
        private readonly IMediaFileStore _mediaFileStore;
        public LetsEncryptController(CommonSettings commonSettings, IMediaFileStore mediaFileStore)
        {
            _commonSettings = commonSettings;
            _mediaFileStore = mediaFileStore;
        }
        public virtual async Task<IActionResult> Index(string fileName)
        {
            if (!_commonSettings.AllowToReadLetsEncryptFile)
                return Content("");

            if (fileName == null)
                return Content("");

            var filepath = _mediaFileStore.Combine("assets", "acme", Path.GetFileName(fileName));
            var fileInfo = await _mediaFileStore.GetFileInfo(filepath);
            if (fileInfo != null)
            {
                return File(await _mediaFileStore.ReadAllText(filepath), "text/plain");
            }
            return Content("");

        }

    }
}
