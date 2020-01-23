using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SiemensApp.Filters
{
    public class SiemensActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var siteIdStr = context.HttpContext.Request.Query["siteId"].FirstOrDefault();
            var siteId = Guid.TryParse(siteIdStr, out var s) ? s : Guid.Empty;
            if (siteId == Guid.Empty)
            {
                context.Result = new RedirectResult("~/Error/Index");
                return;
            }

            var controller = context.Controller as Controller;
            if (controller == null)
            {
                return;
            }

            controller.ViewData["siteId"] = siteId;
        }
        
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // our code after action executes
        }
    }

    public class SiemensAsyncActionFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            bool isValidate = false;
            foreach (var arg in context.ActionArguments)
            {
                if (arg.Key.ToLower() == "siteid" && arg.Value != null)
                {
                    isValidate = true;
                    break;
                }
            }
            if (!isValidate)
            {

                context.Result = new RedirectResult("~/Error/Index");
            }
            // execute any code before the action executes
            var result = await next();
            // execute any code after the action executes
        }
    }
}
