using Zxw.Framework.NetCore.IDbContext;
using Zxw.Framework.NetCore.Repositories;
using Zxw.Framework.UnitTest.TestModels;

namespace Zxw.Framework.UnitTest
{
    public interface ITodoRepository : IRepository<TodoItem, int>
    {
    }

    public class TodoRepository : BaseRepository<TodoItem, int>, ITodoRepository
    {
        public TodoRepository(IDbContextCore dbContext, ISqlOperatorUtility sqlOperator)
            : base(dbContext, sqlOperator)
        {
        }
    }
}
