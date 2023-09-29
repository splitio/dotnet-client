using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class MatchesStringMatcherAsyncTests
    {
        private readonly string rootFilePath;

        public MatchesStringMatcherAsyncTests()
        {
            // This line is to clean the warnings.
            rootFilePath = string.Empty;

#if NET_LATEST
            rootFilePath = @"Resources\";
#endif
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingKeyString()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var result = await matcher.MatchAsync("arrive");

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueOnMatchingKey()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var result = await matcher.MatchAsync(new Key("arrive", "arrive"));

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnNonMatchingKey()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var result = await matcher.MatchAsync("split");

            //Assert
            Assert.IsFalse(result);
        }


        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingLong()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var result = await matcher.MatchAsync(123);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingDate()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var result = await matcher.MatchAsync(DateTime.UtcNow);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingBoolean()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var result = await matcher.MatchAsync(true);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfMatchingSet()
        {
            //Arrange
            var matcher = new MatchesStringMatcher("^a");

            //Act
            var keys = new List<string>
            {
                "test1",
                "test3"
            };

            var result = await matcher.MatchAsync(keys);

            //Assert
            Assert.IsFalse(result);
        }

        [DeploymentItem(@"Resources\regex.txt")]
        [TestMethod]
        public async Task VerifyRegexMatcherAsync()
        {
            await VerifyTestFile($"{rootFilePath}regex.txt", new string[] { "\r\n" });
        }


        private static async Task VerifyTestFile(string file, string[] sepparator)
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
                    var result = await matcher.MatchAsync(item[1]);

                    //Assert
                    Assert.AreEqual(Convert.ToBoolean(item[2]), result, item[0] + "-" + item[1]);
                }

            }
        }
    }
}
