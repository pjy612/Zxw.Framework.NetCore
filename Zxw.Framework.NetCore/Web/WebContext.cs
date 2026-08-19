using Microsoft.AspNetCore.Http;
using System;
using Zxw.Framework.NetCore.Extensions;

namespace Zxw.Framework.NetCore.Web
{
    public class WebContext : IWebContext
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpContext CoreContext =>
            _accessor.HttpContext ??
            throw new InvalidOperationException(
                $"当前没有可用的 HttpContext。请确认已调用 AddHttpContextAccessor，并且仅在 HTTP 请求范围内解析 {nameof(IWebContext)}。");

        public WebContext(IHttpContextAccessor accessor)
        {
            _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        }

        public virtual T GetService<T>()
        {
            return CoreContext.GetService<T>();
        }
    }
}
