﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Grand.Web.Common.Components
{
    [BaseViewComponent(AdminAccess = false)]
    public abstract class BaseViewComponent : ViewComponent
    {
        public new IViewComponentResult View<TModel>(string viewName, TModel model)
        {
            return base.View<TModel>(viewName, model);
        }

        public new IViewComponentResult View<TModel>(TModel model)
        {
            var viewJson = Request?.Headers["X-Response-View"];
            if ((bool)viewJson?.Equals("Json"))
            {
                return new JsonContentViewComponentResult(JsonConvert.SerializeObject(model));
            }
            return base.View<TModel>(model);
        }

        public new IViewComponentResult View(string viewName)
        {
            return base.View(viewName);
        }
    }
}
