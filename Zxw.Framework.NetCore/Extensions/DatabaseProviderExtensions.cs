using System;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Zxw.Framework.NetCore.Extensions
{
    /// <summary>
    /// Provider detection that works with both Pomelo and Oracle MySQL EF providers.
    /// </summary>
    public static class DatabaseProviderExtensions
    {
        public static bool IsMySqlCompatible(this DatabaseFacade database)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            var name = database.ProviderName;
            if (string.IsNullOrEmpty(name)) return false;
            return name.IndexOf("MySql", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("MariaDb", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
