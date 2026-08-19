using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zxw.Framework.NetCore.DbContextCore;
using Zxw.Framework.NetCore.IDbContext;
using Zxw.Framework.NetCore.Options;
using Zxw.Framework.NetCore.Repositories;
using Zxw.Framework.UnitTest.TestModels;

namespace Zxw.Framework.UnitTest
{
    [TestClass]
    public class UnitOfWorkTests
    {
        private static ServiceProvider BuildProvider(bool autoSave)
        {
            var services = new ServiceCollection();
            var option = new DbContextOption
            {
                ConnectionString = $"UnitOfWork_{Guid.NewGuid():N}",
                ModelAssemblyName = typeof(TodoItem).Assembly.GetName().Name,
                IsOutputSql = false,
                EnableLazyLoadingProxy = false,
                EnableNoTracking = false,
                AutoSaveChanges = autoSave
            };
            services.AddSingleton(option);
            services.AddScoped<IDbContextCore>(_ => new InMemoryDbContext(option));
            services.AddScoped<ISqlOperatorUtility, SqlOperatorUtility>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
            services.AddScoped<ITodoRepository, TodoRepository>();
            return services.BuildServiceProvider();
        }

        [TestMethod]
        public void AutoSave_True_PersistsImmediately()
        {
            using var sp = BuildProvider(autoSave: true);
            using var scope = sp.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITodoRepository>();
            var db = scope.ServiceProvider.GetRequiredService<IDbContextCore>();
            db.EnsureCreated();

            repo.Add(new TodoItem { Title = "a", Priority = 1 });

            Assert.AreEqual(1, repo.Count());
        }

        [TestMethod]
        public void AutoSave_False_RequiresUnitOfWork()
        {
            using var sp = BuildProvider(autoSave: false);
            using var scope = sp.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITodoRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var db = scope.ServiceProvider.GetRequiredService<IDbContextCore>();
            db.EnsureCreated();

            repo.Add(new TodoItem { Title = "pending", Priority = 2 });
            Assert.AreEqual(0, repo.Count(x => x.Title == "pending"));

            uow.SaveChanges();
            Assert.AreEqual(1, repo.Count(x => x.Title == "pending"));
        }

        [TestMethod]
        public async Task AutoSave_False_MultipleAdds_CommitOnce()
        {
            using var sp = BuildProvider(autoSave: false);
            using var scope = sp.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITodoRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var db = scope.ServiceProvider.GetRequiredService<IDbContextCore>();
            db.EnsureCreated();

            await repo.AddAsync(new TodoItem { Title = "one", Priority = 1 });
            await repo.AddAsync(new TodoItem { Title = "two", Priority = 2 });
            Assert.AreEqual(0, await repo.CountAsync());

            await uow.SaveChangesAsync();
            Assert.AreEqual(2, await repo.CountAsync());
        }

        [TestMethod]
        public void EditRange_MarksEntitiesModified()
        {
            using var sp = BuildProvider(autoSave: true);
            using var scope = sp.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITodoRepository>();
            var db = scope.ServiceProvider.GetRequiredService<IDbContextCore>();
            db.EnsureCreated();

            var item = new TodoItem { Title = "old", Priority = 1 };
            repo.Add(item);

            item.Title = "new";
            repo.EditRange(new List<TodoItem> { item });

            var loaded = repo.GetSingle(item.Id);
            Assert.AreEqual("new", loaded.Title);
        }

        [TestMethod]
        public void GetByPagination_UsesThenByForSecondarySort()
        {
            using var sp = BuildProvider(autoSave: true);
            using var scope = sp.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITodoRepository>();
            var db = scope.ServiceProvider.GetRequiredService<IDbContextCore>();
            db.EnsureCreated();

            repo.AddRange(new List<TodoItem>
            {
                new TodoItem { Title = "b", Priority = 1 },
                new TodoItem { Title = "a", Priority = 1 },
                new TodoItem { Title = "c", Priority = 2 }
            });

            var page = repo.GetByPagination(null, 10, 1, true, x => x.Priority, x => x.Title).ToList();
            Assert.AreEqual(3, page.Count);
            Assert.AreEqual("a", page[0].Title);
            Assert.AreEqual("b", page[1].Title);
            Assert.AreEqual("c", page[2].Title);
        }

        [TestMethod]
        public async Task ExistAsync_IsTrulyAsync()
        {
            using var sp = BuildProvider(autoSave: true);
            using var scope = sp.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITodoRepository>();
            var db = scope.ServiceProvider.GetRequiredService<IDbContextCore>();
            db.EnsureCreated();

            Assert.IsFalse(await repo.ExistAsync(x => x.Title == "missing"));
            repo.Add(new TodoItem { Title = "exists", Priority = 1 });
            Assert.IsTrue(await repo.ExistAsync(x => x.Title == "exists"));
        }

        [TestMethod]
        public void Repository_IsScoped_SameInstanceInScope()
        {
            using var sp = BuildProvider(autoSave: true);
            using var scope = sp.CreateScope();
            var r1 = scope.ServiceProvider.GetRequiredService<ITodoRepository>();
            var r2 = scope.ServiceProvider.GetRequiredService<ITodoRepository>();
            Assert.AreSame(r1, r2);

            var db1 = scope.ServiceProvider.GetRequiredService<IDbContextCore>();
            var db2 = scope.ServiceProvider.GetRequiredService<IDbContextCore>();
            Assert.AreSame(db1, db2);
        }
    }
}
