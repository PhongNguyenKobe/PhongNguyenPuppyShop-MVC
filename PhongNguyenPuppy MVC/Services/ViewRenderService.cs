using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace PhongNguyenPuppy_MVC.Services
{
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync<TModel>(string viewPath, TModel model);
    }

    public class ViewRenderService : IViewRenderService
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ViewRenderService(IRazorViewEngine viewEngine,
                                 ITempDataProvider tempDataProvider,
                                 IServiceProvider serviceProvider,
                                 IHttpContextAccessor httpContextAccessor)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> RenderToStringAsync<TModel>(string viewPath, TModel model)
        {
            var httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            // Try FindView first (MVC), then GetView (absolute path)
            var viewEngineResult = _viewEngine.FindView(actionContext, viewPath, isMainPage: false);
            if (!viewEngineResult.Success)
            {
                viewEngineResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewPath, isMainPage: false);
            }

            if (!viewEngineResult.Success)
            {
                throw new InvalidOperationException($"View '{viewPath}' was not found. Searched locations: {string.Join(", ", viewEngineResult.SearchedLocations ?? Array.Empty<string>())}");
            }

            var view = viewEngineResult.View;

            await using var sw = new StringWriter();
            var viewData = new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };

            var tempData = new TempDataDictionary(actionContext.HttpContext, _tempDataProvider);
            var viewContext = new ViewContext(actionContext, view, viewData, tempData, sw, new HtmlHelperOptions());

            await view.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}