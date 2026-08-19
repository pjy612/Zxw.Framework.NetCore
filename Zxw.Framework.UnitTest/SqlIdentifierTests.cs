using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zxw.Framework.NetCore.Helpers;

namespace Zxw.Framework.UnitTest
{
    [TestClass]
    public class SqlIdentifierTests
    {
        [TestMethod]
        public void EnsureSafe_AllowsSimpleAndSchemaQualifiedNames()
        {
            SqlIdentifier.EnsureSafe("SysMenu", "viewName");
            SqlIdentifier.EnsureSafe("dbo.SysMenu", "viewName");
        }

        [TestMethod]
        public void EnsureSafe_RejectsInjectionPayloads()
        {
            Assert.ThrowsException<ArgumentException>(() =>
                SqlIdentifier.EnsureSafe("SysMenu; DROP TABLE Users--", "viewName"));
            Assert.ThrowsException<ArgumentException>(() =>
                SqlIdentifier.EnsureSafe("SysMenu WHERE 1=1", "viewName"));
            Assert.ThrowsException<ArgumentException>(() =>
                SqlIdentifier.EnsureSafe(null, "viewName"));
        }

        [TestMethod]
        public void JoinOrderBy_AllowsIdentifierAndDirection()
        {
            var sql = SqlIdentifier.JoinOrderBy(new[] { "CreatedAt DESC", "Id" });
            Assert.AreEqual("CreatedAt DESC,Id", sql);
        }

        [TestMethod]
        public void JoinOrderBy_RejectsExpressionInjection()
        {
            Assert.ThrowsException<ArgumentException>(() =>
                SqlIdentifier.JoinOrderBy(new[] { "Id; DROP TABLE Users" }));
            Assert.ThrowsException<ArgumentException>(() =>
                SqlIdentifier.JoinOrderBy(new[] { "CASE WHEN 1=1 THEN Id ELSE Name END" }));
        }

        [TestMethod]
        public void EnsurePaging_RejectsNonPositiveValues()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => SqlIdentifier.EnsurePaging(0, 10));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => SqlIdentifier.EnsurePaging(1, 0));
        }
    }
}
