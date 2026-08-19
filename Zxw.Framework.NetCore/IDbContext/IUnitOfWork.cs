using System.Threading;
using System.Threading.Tasks;
using Zxw.Framework.NetCore.IoC;

namespace Zxw.Framework.NetCore.IDbContext
{
    /// <summary>
    /// 工作单元：在 AutoSaveChanges=false 时，由调用方统一提交同一 DbContext 上的变更。
    /// </summary>
    public interface IUnitOfWork : IScopedDependency
    {
        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
