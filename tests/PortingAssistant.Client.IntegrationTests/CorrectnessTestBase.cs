using NUnit.Framework;
using System.IO;

namespace PortingAssistant.Client.IntegrationTests
{
    public class CorrectnessTestBase
    {
        protected string _testDirectoryRoot;
        protected string _tmpTestFixturePath_FirstRun;
        protected string _tmpTestFixturePath_SecondRun;
        protected string _testProjectZipPath;

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            _testDirectoryRoot = TestContext.CurrentContext.TestDirectory;

            _tmpTestFixturePath_FirstRun = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()));
            Directory.CreateDirectory(_tmpTestFixturePath_FirstRun);

            _tmpTestFixturePath_SecondRun = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()));
            Directory.CreateDirectory(_tmpTestFixturePath_SecondRun);

            _testProjectZipPath = Path.Combine(
                _testDirectoryRoot,
                "TestProjects",
                "NetFrameworkExample.zip");
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Directory.Delete(_tmpTestFixturePath_FirstRun, true);
        }
    }
}
