using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Z.EntityFramework.Plus;
using Zxw.Framework.NetCore.Attributes;
using Zxw.Framework.NetCore.DbLogProvider;
using Zxw.Framework.NetCore.Extensions;
using Zxw.Framework.NetCore.Helpers;
using Zxw.Framework.NetCore.IDbContext;
using Zxw.Framework.NetCore.IoC;
using Zxw.Framework.NetCore.Models;
using Zxw.Framework.NetCore.Options;

namespace Zxw.Framework.NetCore.DbContextCore
{
    public abstract class BaseDbContext : DbContext, IDbContextCore
    {
        public DbContextOption Option { get; }
        public DatabaseFacade GetDatabase() => Database;
        public T GetService<T>() where T:class,new()
        {
            return this.Database.GetService<T>();
        }

        protected int CommitIfAutoSave()
        {
            return Option.AutoSaveChanges ? SaveChanges() : 0;
        }

        protected Task<int> CommitIfAutoSaveAsync(CancellationToken cancellationToken = default)
        {
            return Option.AutoSaveChanges
                ? SaveChangesAsync(cancellationToken)
                : Task.FromResult(0);
        }

        public new virtual int Add<T>(T entity) where T : class
        {
            base.Add(entity);
            return CommitIfAutoSave();
        }

        protected BaseDbContext(DbContextOption option)
        {
            Option = option ?? ServiceLocator.Resolve<IOptions<DbContextOption>>().Value;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="option"></param>
        protected BaseDbContext(IOptions<DbContextOption> option)
        {
            if(option==null)
                throw new ArgumentNullException(nameof(option));
            if(string.IsNullOrEmpty(option.Value.ConnectionString))
                throw new ArgumentNullException(nameof(option.Value.ConnectionString));
            //if (string.IsNullOrEmpty(option.Value.ModelAssemblyName))
            //    throw new ArgumentNullException(nameof(option.Value.ModelAssemblyName));
            Option = option.Value;
        }

        protected BaseDbContext(DbContextOptions options):base(options)
        {
            Option = ServiceLocator.Resolve<IOptions<DbContextOption>>().Value;
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(Option.IsOutputSql)
            {
                optionsBuilder.UseLoggerFactory(new EntityFrameworkCommandLoggerFactory());
            }

            if (Option.EnableNoTracking)
            {
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }

            if (Option.EnableLazyLoadingProxy)
            {
                optionsBuilder.UseLazyLoadingProxies();
            }

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            MappingEntityTypes(modelBuilder);
            base.OnModelCreating(modelBuilder);
        }

        private void MappingEntityTypes(ModelBuilder modelBuilder)
        {
            if (string.IsNullOrEmpty(Option.ModelAssemblyName))
            {
                return;
            }
            var assembly = Assembly.Load(Option.ModelAssemblyName);
            var types = assembly?.GetTypes().Where(c=>c.GetCustomAttributes<DbContextAttribute>().Any());
            var list = types?.Where(t =>
                t.IsImplement(typeof(IBaseModel<>))||t.IsSubclassOf(typeof(BaseViewModel))).ToList();
            if (list != null && list.Any())
            {
                list.ForEach(t =>
                {
                    var dbContextType = t.GetCustomAttributes<DbContextAttribute>().FirstOrDefault(x=>x.ContextType==GetType());
                    var entityType = modelBuilder.Model.FindEntityType(t);
                    if (entityType == null && null != dbContextType)
                    {                        
                        modelBuilder.Model.AddEntityType(t);
                    }

                    entityType = modelBuilder.Model.FindEntityType(t);
                    var attr = t.GetCustomAttributes<ShardingTableAttribute>().FirstOrDefault();
                    if (attr!=null && entityType!=null)
                    {
                        modelBuilder.Model.FindEntityType(t).SetTableName($"{t.Name}{attr.Splitter}{DateTime.Now.ToString(attr.Suffix)}");
                    }
                });
            }
        }

        public new virtual async Task<int> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
        {
            await base.AddAsync(entity, cancellationToken);
            return await CommitIfAutoSaveAsync(cancellationToken);
        }

        public virtual int AddRange<T>(ICollection<T> entities) where T : class
        {
            base.AddRange(entities);
            return CommitIfAutoSave();
        }

        public virtual async Task<int> AddRangeAsync<T>(ICollection<T> entities, CancellationToken cancellationToken = default) where T : class
        {
            await base.AddRangeAsync(entities, cancellationToken);
            return await CommitIfAutoSaveAsync(cancellationToken);
        }

        public virtual int Count<T>(Expression<Func<T, bool>> @where = null) where T : class
        {
            return where == null ? GetDbSet<T>().Count() : GetDbSet<T>().Count(@where);
        }

        public virtual async Task<int> CountAsync<T>(Expression<Func<T, bool>> @where = null, CancellationToken cancellationToken = default) where T : class
        {
            return await (where == null
                ? GetDbSet<T>().CountAsync(cancellationToken)
                : GetDbSet<T>().CountAsync(@where, cancellationToken));
        }

        public virtual int Delete<T,TKey>(TKey key) where T : BaseModel<TKey>
        {
            var entity = Find<T>(key);
            if (entity == null) return 0;
            Remove(entity);
            return CommitIfAutoSave();
        }

        public virtual bool EnsureCreated()
        {
            return Database.EnsureCreated();
        }

        public virtual async Task<bool> EnsureCreatedAsync()
        {
            return await Database.EnsureCreatedAsync();
        }

        /// <summary>
        /// ExecuteSqlWithNonQuery
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual int ExecuteSqlWithNonQuery(string sql, params object[] parameters)
        {
            return Database.ExecuteSqlRaw(sql, parameters);
        }

        public virtual async Task<int> ExecuteSqlWithNonQueryAsync(string sql, params object[] parameters)
        {
            return await Database.ExecuteSqlRawAsync(sql, parameters);
        }

        /// <summary>
        /// edit an entity.
        /// </summary>
        public virtual int Edit<T>(T entity) where T : class 
        {
            base.Update(entity);
            base.Entry(entity).State = EntityState.Modified;
            return CommitIfAutoSave();
        }

        /// <summary>
        /// edit entities.
        /// </summary>
        public virtual int EditRange<T>(ICollection<T> entities) where T : class
        {
            if (entities == null || entities.Count == 0) return 0;
            foreach (var entity in entities)
            {
                var entry = Entry(entity);
                if (entry.State == EntityState.Detached)
                {
                    GetDbSet<T>().Attach(entity);
                }
                entry.State = EntityState.Modified;
            }
            return CommitIfAutoSave();
        }

        public virtual bool Exist<T>(Expression<Func<T, bool>> @where = null) where T : class
        {
            return @where == null ? GetDbSet<T>().Any() : GetDbSet<T>().Any(@where);
        }

        public virtual IQueryable<T> FilterWithInclude<T>(Func<IQueryable<T>, IQueryable<T>> include, Expression<Func<T, bool>> @where) where T : class
        {
            var result = GetDbSet<T>().AsQueryable();
            if (where != null)
                result = GetDbSet<T>().Where(where);
            if (include != null)
                result = include(result);
            return result;
        }

        public virtual async Task<bool> ExistAsync<T>(Expression<Func<T, bool>> @where = null, CancellationToken cancellationToken = default) where T : class
        {
            return await (where == null
                ? GetDbSet<T>().AnyAsync(cancellationToken)
                : GetDbSet<T>().AnyAsync(where, cancellationToken));
        }

        public virtual T Find<T>(object key) where T : class
        {
            return base.Find<T>(key);
        }
        public virtual T Find<T,TKey>(TKey key) where T : BaseModel<TKey>
        {
            return base.Find<T>(key);
            //return GetByCompileQuery<T, TKey>(key);
        }

        public virtual async Task<T> FindAsync<T>(object key) where T : class
        {
            return await base.FindAsync<T>(key);
        }
        public virtual async Task<T> FindAsync<T,TKey>(TKey key) where T : BaseModel<TKey>
        {
            return await base.FindAsync<T>(key);
            //return await GetByCompileQueryAsync<T, TKey>(key);
        }

        public virtual IQueryable<T> Get<T>(Expression<Func<T, bool>> @where = null, bool asNoTracking = false) where T : class
        {
            var query = GetDbSet<T>().AsQueryable();
            if (where != null)
                query = query.Where(where);
            if (asNoTracking)
                query = query.AsNoTracking();
            return query;
        }

        public virtual List<IEntityType> GetAllEntityTypes()
        {
            return Model.GetEntityTypes().ToList();
        }
        public virtual DbSet<T> GetDbSet<T>() where T : class
        {
            if (Model.FindEntityType(typeof(T)) != null)
                return Set<T>();
            throw new Exception($"类型{typeof(T).Name}未在数据库上下文中注册，请先在DbContextOption设置ModelAssemblyName以将所有实体类型注册到数据库上下文中。");
        }

        public virtual T GetSingleOrDefault<T>(Expression<Func<T, bool>> @where = null) where T : class
        {
            return where == null ? GetDbSet<T>().SingleOrDefault() : GetDbSet<T>().SingleOrDefault(where);
            //return FirstOrDefaultByCompileQuery<T>(where);
        }

        public virtual async Task<T> GetSingleOrDefaultAsync<T>(Expression<Func<T, bool>> @where = null, CancellationToken cancellationToken = default) where T : class
        {
            return await (where == null
                ? GetDbSet<T>().SingleOrDefaultAsync(cancellationToken)
                : GetDbSet<T>().SingleOrDefaultAsync(where, cancellationToken));
        }

        /// <summary>
        /// update data by columns.
        /// </summary>
        public virtual int Update<T>(T model, params string[] updateColumns) where T : class
        {
            if (updateColumns != null && updateColumns.Length > 0)
            {
                if (Entry(model).State == EntityState.Added ||
                    Entry(model).State == EntityState.Detached) GetDbSet<T>().Attach(model);
                foreach (var propertyName in updateColumns)
                {
                    Entry(model).Property(propertyName).IsModified = true;
                }
            }
            else
            {
                Entry(model).State = EntityState.Modified;
            }
            return CommitIfAutoSave();
        }

        public virtual int Update<T>(Expression<Func<T, bool>> @where, Expression<Func<T,T>> updateFactory) where T : class
        {
            return GetDbSet<T>().Where(where).Update(updateFactory);
        }

        public virtual async Task<int> UpdateAsync<T>(Expression<Func<T, bool>> @where, Expression<Func<T,T>> updateFactory, CancellationToken cancellationToken = default) where T : class
        {
            return await GetDbSet<T>().Where(where).UpdateAsync(updateFactory, cancellationToken);
        }

        /// <summary>
        /// delete by query.
        /// </summary>
        public virtual int Delete<T>(Expression<Func<T, bool>> @where) where T : class
        {
            return GetDbSet<T>().Where(@where).Delete();
        }

        public virtual async Task<int> DeleteAsync<T>(Expression<Func<T, bool>> @where, CancellationToken cancellationToken = default) where T : class
        {
            return await GetDbSet<T>().Where(@where).DeleteAsync(cancellationToken);
        }

        /// <summary>
        /// bulk insert by sqlbulkcopy, and with transaction.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="entities"></param>
        /// <param name="destinationTableName"></param>
        public virtual void BulkInsert<T>(IList<T> entities, string destinationTableName = null) where T : class 
        {
            if (!Database.IsSqlServer() && !Database.IsMySqlCompatible())
             throw new NotSupportedException("This method only supports for SQL Server or MySql.");
        }
        [Obsolete]
        public virtual List<TView> SqlQuery<T, TView>(string sql, params object[] parameters) where T : class
        {
            return GetDbSet<T>().FromSqlRaw(sql).Cast<TView>().ToList();
        }

        public virtual List<TView> SqlQuery<TView>(string sql, int cmdTimeout = 30, params object[] parameters) 
        {
            var result = new List<TView>();
            var connection = Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    result = reader.ToList<TView>().ToList();

                    reader.Close();
                }
            }
            connection.Close();

            return result;
        }

        public virtual PaginationResult SqlQueryByPagination<T, TView>(string sql, string[] orderBys, int pageIndex, int pageSize,
            Action<TView> eachAction = null) where T : class where TView : class
        {
            throw new NotImplementedException();
        }

        public virtual DataTable GetDataTable(string sql, int cmdTimeout = 30, params DbParameter[] parameters) =>
            GetDataTables(sql, cmdTimeout, parameters)?.FirstOrDefault();

        public virtual PaginationResult SqlQueryByPagination<T>(string sql, string[] orderBys, int pageIndex, int pageSize,
            params DbParameter[] parameters) where T:class, new()
        {
            throw new NotImplementedException();
        }

        public virtual List<DataTable> GetDataTables(string sql, int cmdTimeout = 30, params DbParameter[] parameters)
        {
            var dts = new List<DataTable>();
            //TODO： connection 不能dispose 或者 用using，否则下次获取connection会报错提示“the connectionstring property has not been initialized。”
            var connection = Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandTimeout = cmdTimeout;
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    var dt = reader.Fill();

                    dts.Add(dt);

                    while (reader.NextResult())
                    {
                        dt = reader.Fill();
                        dts.Add(dt);
                    }
                    reader.Close();
                }
            }
            connection.Close();

            return dts;
        }

        public T GetByCompileQuery<T, TKey>(TKey id) where T : BaseModel<TKey>
        {
            return EF.CompileQuery((DbContext context, TKey id) => context.Set<T>().Find(id))(this, id);
        }
        public Task<T> GetByCompileQueryAsync<T, TKey>(TKey id) where T : BaseModel<TKey>
        {
            return EF.CompileAsyncQuery((DbContext context, TKey id) => context.Set<T>().Find(id))(this, id);
        }

        public IList<T> GetByCompileQuery<T>(Expression<Func<T, bool>> filter) where T : class
        {
            if (filter == null) filter = m => true;
            return EF.CompileQuery((DbContext context) => context.Set<T>().AsNoTracking().Where(filter).ToList())(this);
        }
        public Task<List<T>> GetByCompileQueryAsync<T>(Expression<Func<T, bool>> filter) where T : class
        {
            if (filter == null) filter = m => true;
            return EF.CompileAsyncQuery((DbContext context) => context.Set<T>().AsNoTracking().Where(filter).ToList())(this);
        }

        public T FirstOrDefaultByCompileQuery<T>(Expression<Func<T, bool>> filter) where T : class
        {
            if (filter == null) filter = m => true;
            return EF.CompileQuery((DbContext context) => context.Set<T>().AsNoTracking().FirstOrDefault(filter))(this);
        }
        public Task<T> FirstOrDefaultByCompileQueryAsync<T>(Expression<Func<T, bool>> filter) where T : class
        {
            if (filter == null) filter = m => true;
            return EF.CompileAsyncQuery((DbContext context) => context.Set<T>().AsNoTracking().FirstOrDefault(filter))(this);
        }
        public T FirstOrDefaultWithTrackingByCompileQuery<T>(Expression<Func<T, bool>> filter) where T : class
        {
            if (filter == null) filter = m => true;
            return EF.CompileQuery((DbContext context) => context.Set<T>().FirstOrDefault(filter))(this);
        }
        public Task<T> FirstOrDefaultWithTrackingByCompileQueryAsync<T>(Expression<Func<T, bool>> filter) where T : class
        {
            if (filter == null) filter = m => true;
            return EF.CompileAsyncQuery((DbContext context) => context.Set<T>().FirstOrDefault(filter))(this);
        }
        public int CountByCompileQuery<T>(Expression<Func<T, bool>> filter) where T:class
        {
            if (filter == null) filter = m => true;
            return EF.CompileQuery((DbContext context) => context.Set<T>().Count(filter))(this);
        }
        public Task<int> CountByCompileQueryAsync<T>(Expression<Func<T, bool>> filter) where T : class
        {
            if (filter == null) filter = m => true;
            return EF.CompileAsyncQuery((DbContext context) => context.Set<T>().Count(filter))(this);
        }
    }
}