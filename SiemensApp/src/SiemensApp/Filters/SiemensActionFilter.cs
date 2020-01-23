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
            bool isValidate = false;
            foreach(var arg in context.ActionArguments)
            {
                if(arg.Key.ToLower() == "siteid" && arg.Value != null)
                {                    
                    isValidate = true;
                    break;
                }
            }
            if(!isValidate)
            {
                context.Result = new RedirectResult("~/Error/Index");                
            }
            // our code before action executes
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
