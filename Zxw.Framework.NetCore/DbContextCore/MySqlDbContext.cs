using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Zxw.Framework.NetCore.Helpers;
using Zxw.Framework.NetCore.IDbContext;
using Zxw.Framework.NetCore.Options;
#if !NET10_0
using MySqlConnector;
using Zxw.Framework.NetCore.Extensions;
#endif

namespace Zxw.Framework.NetCore.DbContextCore
{
    public class MySqlDbContext: BaseDbContext, IMySqlDbContext
    {
        public MySqlDbContext(DbContextOption option) : base(option)
        {

        }
        public MySqlDbContext(IOptions<DbContextOption> option) : base(option)
        {
        }

        public MySqlDbContext(DbContextOptions options) : base(options){}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if NET10_0
            optionsBuilder.UseMySQL(Option.ConnectionString);
#else
            optionsBuilder.UseMySql(Option.ConnectionString, ServerVersion.AutoDetect(Option.ConnectionString));
#endif
            base.OnConfiguring(optionsBuilder);
        }

        public override void BulkInsert<T>(IList<T> entities, string destinationTableName = null)
        {
            if (entities == null || !entities.Any()) return;
            if (string.IsNullOrEmpty(destinationTableName))
            {
                var mappingTableName = typeof(T).GetCustomAttribute<TableAttribute>()?.Name;
                destinationTableName = string.IsNullOrEmpty(mappingTableName) ? typeof(T).Name : mappingTableName;
            }
            SqlIdentifier.EnsureSafe(destinationTableName, nameof(destinationTableName));
#if NET10_0
            // Oracle MySQL EF 提供程序与 MySqlConnector BulkLoader 不兼容，退化为跟踪插入。
            AddRange(entities);
#else
            MySqlBulkInsert(entities, destinationTableName);
#endif
        }

#if !NET10_0
        private void MySqlBulkInsert<T>(IList<T> entities, string destinationTableName) where T : class
        {
            var tmpDir = Path.Combine(AppContext.BaseDirectory, "Temp");
            Directory.CreateDirectory(tmpDir);
            var csvFileName = Path.Combine(tmpDir, $"{Guid.NewGuid():N}.csv");
            try
            {
                var separator = ",";
                entities.SaveToCsv(csvFileName, separator);
                var conn = (MySqlConnection) Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                var bulk = new MySqlBulkLoader(conn)
                {
                    NumberOfLinesToSkip = 0,
                    TableName = destinationTableName,
                    FieldTerminator = separator,
                    FieldQuotationCharacter = '"',
                    EscapeCharacter = '"',
                    LineTerminator = "\r\n",
                    FileName = csvFileName,
                    Local = true
                };
                bulk.Load();
            }
            finally
            {
                if (File.Exists(csvFileName))
                {
                    File.Delete(csvFileName);
                }
            }
        }
#endif
    }
}
