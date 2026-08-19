using System;
using System.Threading;
using System.Threading.Tasks;
using Zxw.Framework.NetCore.IDbContext;

namespace Zxw.Framework.NetCore.DbContextCore
{
    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly IDbContextCore _dbContext;

        public EfUnitOfWork(IDbContextCore dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public int SaveChanges() => _dbContext.SaveChanges();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);
    }
}
