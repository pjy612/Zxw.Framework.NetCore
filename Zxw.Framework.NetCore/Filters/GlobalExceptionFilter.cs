using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Zxw.Framework.NetCore.Helpers;

namespace Zxw.Framework.NetCore.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            var type = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
            Log4NetHelper.WriteError(type, filterContext.Exception);

            filterContext.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Detail = filterContext.Exception.GetType().Name
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            filterContext.ExceptionHandled = true;
        }
    }
}
