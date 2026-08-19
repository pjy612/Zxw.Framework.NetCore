using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zxw.Framework.NetCore.Extensions;
using Zxw.Framework.NetCore.Web;

namespace Zxw.Framework.UnitTest
{
    [TestClass]
    public class WebContextLifetimeTests
    {
        [TestMethod]
        public void WebContext_IsScoped_AndReadsCurrentHttpContext()
        {
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            services.AddDefaultWebContext();
            using var sp = services.BuildServiceProvider();

            var accessor = sp.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = new DefaultHttpContext { TraceIdentifier = "req-1" };

            using (var scope1 = sp.CreateScope())
            {
                var ctx = scope1.ServiceProvider.GetRequiredService<IWebContext>();
                Assert.AreEqual("req-1", ctx.CoreContext.TraceIdentifier);

                accessor.HttpContext = new DefaultHttpContext { TraceIdentifier = "req-2" };
                Assert.AreEqual("req-2", ctx.CoreContext.TraceIdentifier);
            }
        }

        [TestMethod]
        public void WebContext_Throws_WhenNoHttpContext()
        {
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            services.AddDefaultWebContext();
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<IWebContext>();
            Assert.ThrowsException<InvalidOperationException>(() => _ = ctx.CoreContext);
        }
    }
}
