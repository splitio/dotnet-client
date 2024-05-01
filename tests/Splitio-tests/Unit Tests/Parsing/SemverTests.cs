using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.SemverImp;
using System.IO;

namespace Splitio_Tests.Unit_Tests.Parsing
{
    [TestClass]
    public class SemverTests
    {
        private readonly string rootFilePath;

        public SemverTests()
        {
            rootFilePath = string.Empty;

#if NET8_0
            rootFilePath = @"Resources\";
#endif
        }

        [DeploymentItem(@"Resources\valid_semantic_versions.csv")]
        [TestMethod]
        public void GreaterThanOrEqualTo()
        {
            using (var reader = new StreamReader($"{rootFilePath}valid_semantic_versions.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var items = line.Split(',');

                    var version1 = Semver.Build(items[0]);
                    var version2 = Semver.Build(items[1]);

                    Assert.IsTrue(version1.Compare(version2) >= 0);
                    Assert.IsFalse(version2.Compare(version1) >= 0);
                    Assert.IsTrue(version1.Compare(version1) >= 0);
                    Assert.IsTrue(version2.Compare(version2) >= 0);
                }
            }
        }

        [DeploymentItem(@"Resources\valid_semantic_versions.csv")]
        [TestMethod]
        public void LessThanOrEqualTo()
        {
            using (var reader = new StreamReader($"{rootFilePath}valid_semantic_versions.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var items = line.Split(',');

                    var version1 = Semver.Build(items[0]);
                    var version2 = Semver.Build(items[1]);

                    Assert.IsFalse(version1.Compare(version2) <= 0);
                    Assert.IsTrue(version2.Compare(version1) <= 0);
                    Assert.IsTrue(version1.Compare(version1) <= 0);
                    Assert.IsTrue(version2.Compare(version2) <= 0);
                }
            }
        }

        [DeploymentItem(@"Resources\invalid_semantic_versions.csv")]
        [TestMethod]
        public void InvalidFormats()
        {
            using (var reader = new StreamReader($"{rootFilePath}invalid_semantic_versions.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    Assert.IsNull(Semver.Build(line), line);
                }
            }
        }

        [DeploymentItem(@"Resources\equal_to_semver.csv")]
        [TestMethod]
        public void EqualTo()
        {
            using (var reader = new StreamReader($"{rootFilePath}equal_to_semver.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var items = line.Split(',');

                    var semver1 = Semver.Build(items[0]);
                    var semver2 = Semver.Build(items[1]);

                    Assert.AreEqual(bool.Parse(items[2]), semver1.EqualTo(semver2));
                }
            }
        }

        [DeploymentItem(@"Resources\between_semver.csv")]
        [TestMethod]
        public void Between()
        {
            using (var reader = new StreamReader($"{rootFilePath}between_semver.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var items = line.Split(',');

                    var version1 = Semver.Build(items[0]);
                    var version2 = Semver.Build(items[1]);
                    var version3 = Semver.Build(items[2]);

                    var result = version2.Compare(version1) >= 0 && version2.Compare(version3) <= 0;

                    Assert.AreEqual(bool.Parse(items[3]), result);
                }
            }
        }
    }
}
