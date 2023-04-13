using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Splitio_tests.Unit_Tests.Matchers
{
    [TestClass]
    public class MatchesStringMatcherTests
    {
        private readonly string rootFilePath;

        public MatchesStringMatcherTests()
        {
            // This line is to clean the warnings.
            rootFilePath = string.Empty;

#if NET_LATEST
            rootFilePath = @"Resources\";
#endif
        }

        [TestMethod]
        public async Task MatchShouldReturnTrueOnMatchingKeyString()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var result = await matcher.Match("arrive");

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnTrueOnMatchingKey()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var result = await matcher.Match(new Key("arrive", "arrive"));

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseOnNonMatchingKey()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var result = await matcher.Match("split");

            //Assert
            Assert.IsFalse(result);
        }


        [TestMethod]
        public void MatchShouldReturnFalseIfMatchingLong()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var result = matcher.Match(123);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfMatchingDate()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var result = matcher.Match(DateTime.UtcNow);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfMatchingBoolean()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var result = matcher.Match(true);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfMatchingSet()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var keys = new List<string>
            {
                "test1",
                "test3"
            };

            var result = matcher.Match(keys);

            //Assert
            Assert.IsFalse(result);
        }

        [DeploymentItem(@"Resources\regex.txt")]
        [TestMethod]
        public async Task VerifyRegexMatcher()
        {
            await VerifyTestFile($"{rootFilePath}regex.txt", new string[] { "\r\n" });
        }


        private async Task VerifyTestFile(string file, string[] sepparator)
        {
            //Arrange
            var fileContent = File.ReadAllText(file);
            var contents = fileContent.Split(sepparator, StringSplitOptions.None);
            var csv = from line in contents
                      select line.Split('#').ToArray();

            var results = new List<string>();
            //Act
            foreach (string[] item in csv)
            {
                if (item.Length == 3)
                {
                    //Arrange
                    var matcher = new MatchesStringMatcher(item[0]);

                    //Act
                    var result = await matcher.Match(item[1]);

                    //Assert
                    Assert.AreEqual(Convert.ToBoolean(item[2]), result, item[0] + "-" + item[1]);
                }

            }
        }
    }
}
