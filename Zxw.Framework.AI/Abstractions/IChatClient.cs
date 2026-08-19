using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zxw.Framework.AI.Abstractions
{
    /// <summary>
    /// 聊天补全客户端抽象（默认实现为 OrcaRouter）。
    /// </summary>
    public interface IChatClient
    {
        Task<ChatCompletionResponse> CompleteAsync(
            ChatCompletionRequest request,
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<string> CompleteStreamingAsync(
            ChatCompletionRequest request,
            CancellationToken cancellationToken = default);
    }
}
