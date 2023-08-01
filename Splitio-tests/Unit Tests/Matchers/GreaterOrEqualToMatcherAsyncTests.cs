using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.CommonLibraries;
using Splitio.Domain;
using Splitio.Services.Parsing;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Matchers
{
    [TestClass]
    public class GreaterOrEqualToMatcherAsyncTests
    {
        [TestMethod]
        public async Task MatchAsyncNumberSuccesfully()
        {
            //Arrange
            var matcher = new GreaterOrEqualToMatcher(DataTypeEnum.NUMBER, 1000001);

            //Act
            var result1 = await matcher.MatchAsync(170000990);
            var result2 = await matcher.MatchAsync(545345);
            var result3 = await matcher.MatchAsync(1000001);

            //Assert
            Assert.IsTrue(result1);
            Assert.IsFalse(result2);
            Assert.IsTrue(result3);
        }

        [TestMethod]
        public async Task MatchAsyncNumberShouldReturnFalseOnInvalidNumberKey()
        {
            //Arrange
            var matcher = new GreaterOrEqualToMatcher(DataTypeEnum.NUMBER, 1000001);

            //Act
            var result = await matcher.MatchAsync(new Key("1aaaaa0", "1aaaaa0"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncDateSuccesfully()
        {
            //Arrange
            var matcher = new GreaterOrEqualToMatcher(DataTypeEnum.DATETIME, 1470960000000);

            //Act
            var result = await matcher.MatchAsync("1470970000000".ToDateTime().Value);
            var result1 = await matcher.MatchAsync("1470910000000".ToDateTime().Value);
            var result2 = await matcher.MatchAsync("1470960000000".ToDateTime().Value);

            //Assert
            Assert.IsTrue(result);
            Assert.IsFalse(result1);
            Assert.IsTrue(result2);
        }

        [TestMethod]
        public async Task MatchAsyncDateTruncateToMinutesSuccesfully()
        {
            //Arrange
            var date1 = "1482207323000".ToDateTime().Value;
            date1 = date1.AddSeconds(14);
            date1 = date1.AddMilliseconds(324);

            var date2 = "1482207383000".ToDateTime().Value;
            date2 = date2.AddSeconds(12);
            date2 = date2.AddMilliseconds(654);

            var date3 = "1470960065443".ToDateTime().Value;
            date3 = date3.AddSeconds(11);
            date3 = date3.AddMilliseconds(456);

            var matcher = new GreaterOrEqualToMatcher(DataTypeEnum.DATETIME, 1482207323000);

            //Act
            var result = await matcher.MatchAsync(date1);
            var result1 = await matcher.MatchAsync(date2);
            var result2 = await matcher.MatchAsync(date3);

            //Assert
            Assert.IsTrue(result);
            Assert.IsTrue(result1);
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public async Task MatchAsyncDateShouldReturnFalseOnInvalidDateKey()
        {
            //Arrange
            var matcher = new GreaterOrEqualToMatcher(DataTypeEnum.DATETIME, 1470960000000);

            //Act
            var result = await matcher.MatchAsync(new Key("1aaa0000000", "1aaa0000000"));

            //Assert
            Assert.IsFalse(result);

        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnInvalidDataTypeKey()
        {
            //Arrange
            var matcher = new GreaterOrEqualToMatcher(DataTypeEnum.STRING, 1470960000000);

            //Act
            var result = await matcher.MatchAsync(new Key("abcd", "abcd"));

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfNullOrEmptyKey()
        {
            //Arrange
            var matcher = new GreaterOrEqualToMatcher(DataTypeEnum.DATETIME, 1470960000000);

            //Act
            var result = await matcher.MatchAsync(new Key("", ""));
            var result2 = await matcher.MatchAsync(new Key((string)null, null));

            //Assert
            Assert.IsFalse(result);
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public async Task MatchAsyncNumberShouldReturnFalseOnInvalidNumber()
        {
            //Arrange
            var matcher = new GreaterOrEqualToMatcher(DataTypeEnum.NUMBER, 1000001);

            //Act
            var result = await matcher.MatchAsync("1aaaaa0");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncDateShouldReturnFalseOnInvalidDate()
        {
            //Arrange
            var matcher = new GreaterOrEqualToMatcher(DataTypeEnum.DATETIME, 1470960000000);

            //Act
            var result = await matcher.MatchAsync("1aaa0000000");

            //Assert
            Assert.IsFalse(result);

        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnInvalidDataType()
        {
            //Arrange
            var matcher = new GreaterOrEqualToMatcher(DataTypeEnum.STRING, 1470960000000);

            //Act
            var result = await matcher.MatchAsync("abcd");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseOnBooleanParameter()
        {
            //Arrange
            var matcher = new GreaterOrEqualToMatcher(DataTypeEnum.DATETIME, 1470960000000);

            //Act
            var result = await matcher.MatchAsync(true);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task MatchAsyncShouldReturnFalseIfNullOrEmpty()
        {
            //Arrange
            var matcher = new GreaterOrEqualToMatcher(DataTypeEnum.DATETIME, 1470960000000);

            //Act
            var result = await matcher.MatchAsync("");
            var result2 = await matcher.MatchAsync((string)null);

            //Assert
            Assert.IsFalse(result);
            Assert.IsFalse(result2);
        }
    }
}
