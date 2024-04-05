using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Parsing.Classes;
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

                    Assert.IsTrue(version1.GreaterThanOrEqualTo(version2));
                    Assert.IsFalse(version2.GreaterThanOrEqualTo(version1));
                    Assert.IsTrue(version1.GreaterThanOrEqualTo(version1));
                    Assert.IsTrue(version2.GreaterThanOrEqualTo(version2));
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

                    Assert.IsFalse(version1.LessThanOrEqualTo(version2));
                    Assert.IsTrue(version2.LessThanOrEqualTo(version1));
                    Assert.IsTrue(version1.LessThanOrEqualTo(version1));
                    Assert.IsTrue(version2.LessThanOrEqualTo(version2));
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

                    Assert.IsNull(Semver.Build(line));
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

                    var version1 = Semver.Build(items[0]);
                    var version2 = Semver.Build(items[1]);

                    Assert.AreEqual(bool.Parse(items[2]), version1.EqualTo(version2));
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

                    Assert.AreEqual(bool.Parse(items[3]), version2.Between(version1, version3));
                }
            }
        }
    }
}
