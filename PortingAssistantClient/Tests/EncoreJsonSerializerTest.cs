using System.Collections.Generic;
using EncoreApiElectron;
using NUnit.Framework;

namespace Tests
{
    public class EncoreJsonSerializerTest
    {
        [Test]
        public void TestSerialize()
        {
            Foo foo = new Foo
            {
                AnIntegerProperty = 42,
                HTMLString = "<html></html>",
                Dictionary = new Dictionary<string, string>
                {
                    { "ALLCAPS", "1" },
                    { "FOO", "2" },
                    { "Bar", "3" },
                    { "baz", "4" }
                }
            };

            EncoreJsonSerializer serializer = new EncoreJsonSerializer();

            var result = serializer.Serialise(foo);

            Assert.AreEqual("{\"anIntegerProperty\":42,\"htmlString\":\"<html></html>\",\"dictionary\":{\"ALLCAPS\":\"1\",\"FOO\":\"2\",\"Bar\":\"3\",\"baz\":\"4\"}}", result);
        }

        class Foo
        {
            public int AnIntegerProperty { get; set; }
            public string HTMLString { get; set; }
            public Dictionary<string, string> Dictionary { get; set; }
        }
    }
}
