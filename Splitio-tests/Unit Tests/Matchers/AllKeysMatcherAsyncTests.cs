using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests
{
    [TestClass]
    public class AllKeysMatcherAsyncTests
    {
        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueForAnyKey()
        {
            //Arrange
            var matcher = new AllKeysMatcher();

            //Act
            var result = await matcher.MatchAsync(new Key("test", "test"));

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfNull()
        {
            //Arrange
            var matcher = new AllKeysMatcher();

            //Act
            var result2 = await matcher.MatchAsync(new Key(null, null));

            //Assert
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnTrueForAnyStringKey()
        {
            //Arrange
            var matcher = new AllKeysMatcher();

            //Act
            var result = await matcher.MatchAsync("test");

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfNullString()
        {
            //Arrange
            var matcher = new AllKeysMatcher();

            //Act
            var result2 = await matcher.MatchAsync((string)null);

            //Assert
            Assert.IsFalse(result2);
        }
    }
}
