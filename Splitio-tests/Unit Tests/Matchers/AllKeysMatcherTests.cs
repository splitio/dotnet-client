using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests
{
    [TestClass]
    public class AllKeysMatcherTests
    {
        [TestMethod]
        public async Task MatchShouldReturnTrueForAnyKey()
        {
            //Arrange
            var matcher = new AllKeysMatcher();

            //Act
            var result = await matcher.Match(new Key("test", "test"));

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchShouldReturnFalseIfNull()
        {
            //Arrange
            var matcher = new AllKeysMatcher();

            //Act
            var result2 = await matcher.Match(new Key((string)null, null));

            //Assert
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public void MatchShouldReturnTrueForAnyStringKey()
        {
            //Arrange
            var matcher = new AllKeysMatcher();

            //Act
            var result = matcher.Match("test");

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchShouldReturnFalseIfNullString()
        {
            //Arrange
            var matcher = new AllKeysMatcher();

            //Act
            var result2 = matcher.Match((string)null);

            //Assert
            Assert.IsFalse(result2);
        }
    }
}
