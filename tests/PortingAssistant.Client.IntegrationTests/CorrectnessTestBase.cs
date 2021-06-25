using NUnit.Framework;
using System.IO;

namespace PortingAssistant.Client.IntegrationTests
{
    public class CorrectnessTestBase
    {
        protected string testDirectoryRoot;
        protected string tmpTestFixturePath;
        protected string testProjectZipPath;

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            testDirectoryRoot = TestContext.CurrentContext.TestDirectory;

            tmpTestFixturePath = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()));
            Directory.CreateDirectory(tmpTestFixturePath);

            testProjectZipPath = Path.Combine(
                testDirectoryRoot,
                "TestProjects",
                "NetFrameworkExample.zip");
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Directory.Delete(tmpTestFixturePath, true);
        }
    }
}
