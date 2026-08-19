using System;
using System.Text.RegularExpressions;

namespace Zxw.Framework.NetCore.Helpers
{
    /// <summary>
    /// Validates SQL identifiers before they are interpolated into dynamic SQL.
    /// Does not make raw SQL APIs safe; it only rejects values that are not simple identifiers.
    /// </summary>
    public static class SqlIdentifier
    {
        private static readonly Regex Identifier = new Regex(
            @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex OrderByItem = new Regex(
            @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)?(\s+(ASC|DESC))?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static void EnsureSafe(string identifier, string paramName)
        {
            if (string.IsNullOrWhiteSpace(identifier) || !Identifier.IsMatch(identifier.Trim()))
            {
                throw new ArgumentException(
                    "Value must be a simple SQL identifier (letters, digits, underscore; optional schema).",
                    paramName);
            }
        }

        public static void EnsurePaging(int pageIndex, int pageSize)
        {
            if (pageIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageIndex), "pageIndex must be greater than or equal to 1.");
            }

            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "pageSize must be greater than or equal to 1.");
            }
        }

        public static string JoinOrderBy(string[] orderBys)
        {
            if (orderBys == null || orderBys.Length == 0)
            {
                throw new ArgumentException("At least one ORDER BY column is required.", nameof(orderBys));
            }

            var sanitized = new string[orderBys.Length];
            for (var i = 0; i < orderBys.Length; i++)
            {
                var item = orderBys[i]?.Trim();
                if (string.IsNullOrEmpty(item) || !OrderByItem.IsMatch(item))
                {
                    throw new ArgumentException(
                        $"Invalid ORDER BY expression: '{orderBys[i]}'. Only identifiers with optional ASC/DESC are allowed.",
                        nameof(orderBys));
                }

                sanitized[i] = item;
            }

            return string.Join(",", sanitized);
        }
    }
}
