using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zxw.Framework.NetCore.Helpers;

namespace Zxw.Framework.UnitTest
{
    [TestClass]
    public class JsonConvertorTests
    {
        private class Sample
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        [TestMethod]
        public void RoundTrip_SerializeDeserialize()
        {
            var json = JsonConvertor.Serialize(new Sample { Name = "Ada", Age = 36 });
            var restored = JsonConvertor.Deserialize<Sample>(json);
            Assert.AreEqual("Ada", restored.Name);
            Assert.AreEqual(36, restored.Age);
        }

        [TestMethod]
        public void Deserialize_IsCaseInsensitive()
        {
            var restored = JsonConvertor.Deserialize<Sample>("{\"name\":\"Bob\",\"age\":20}");
            Assert.AreEqual("Bob", restored.Name);
            Assert.AreEqual(20, restored.Age);
        }
    }
}
