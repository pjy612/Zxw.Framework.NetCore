using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Zxw.Framework.AI.Abstractions;
using Zxw.Framework.AI.Clients;
using Zxw.Framework.AI.Options;

namespace Zxw.Framework.AI.Extensions
{
    public static class OrcaRouterServiceCollectionExtensions
    {
        /// <summary>
        /// 注册 OrcaRouter 为默认 <see cref="IChatClient"/>（内置 LLM 网关选项）。
        /// </summary>
        public static IServiceCollection AddOrcaRouter(
            this IServiceCollection services,
            Action<OrcaRouterOptions> configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddOptions<OrcaRouterOptions>();
            if (configure != null)
                services.Configure(configure);

            services.AddHttpClient(OrcaRouterOptions.HttpClientName)
                .ConfigureHttpClient((sp, client) =>
                {
                    var options = sp.GetRequiredService<IOptions<OrcaRouterOptions>>().Value;
                    var baseUrl = (options.BaseUrl ?? OrcaRouterOptions.DefaultBaseUrl).TrimEnd('/') + "/";
                    client.BaseAddress = new Uri(baseUrl);
                    client.Timeout = options.Timeout <= TimeSpan.Zero
                        ? TimeSpan.FromSeconds(100)
                        : options.Timeout;
                });

            services.TryAddSingleton<IChatClient, OrcaRouterChatClient>();
            return services;
        }
    }
}
