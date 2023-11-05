using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Grand.Web.Common.Components
{
    public abstract class BaseViewComponent : ViewComponent
    {
        public new IViewComponentResult View<TModel>(string viewName, TModel model)
        {
            return base.View<TModel>(viewName, model);
        }

        public new IViewComponentResult View<TModel>(TModel model)
        {
            if(Request?.ContentType == "application/json")
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
