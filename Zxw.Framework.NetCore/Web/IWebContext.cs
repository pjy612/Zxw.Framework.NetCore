using Microsoft.AspNetCore.Http;
using Zxw.Framework.NetCore.IoC;

namespace Zxw.Framework.NetCore.Web
{
    public interface IWebContext: IScopedDependency
    {
        HttpContext CoreContext { get; }
        T GetService<T>();
    }
}
