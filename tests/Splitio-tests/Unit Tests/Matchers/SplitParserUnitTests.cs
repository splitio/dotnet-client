using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Parsing.Classes;
using System.Collections.Generic;

namespace Splitio_Tests.Unit_Tests
{
    [TestClass]
    public class SplitParserUnitTests
    {
        [TestMethod]
        public void ParseSuccessfullyWhenNonSpecifiedAlgorithm()
        {
            //Arrange
            Split split = new Split
            {
                Name = "test1",
                Seed = 2323,
                Status = "ACTIVE",
                Killed = false,
                DefaultTreatment = "off",
                ChangeNumber = 232323,
                TrafficTypeName = "user",
                Conditions = new List<Condition>()
            };

            var parser = new InMemorySplitParser(null, null);

            //Act
            var parsedSplit = parser.Parse(split);

            //Assert
            Assert.IsNotNull(parsedSplit);
            Assert.AreEqual(split.Name, parsedSplit.Name);
            Assert.AreEqual(split.Seed, parsedSplit.Seed);
            Assert.AreEqual(split.Killed, parsedSplit.Killed);
            Assert.AreEqual(split.DefaultTreatment, parsedSplit.DefaultTreatment);
            Assert.AreEqual(split.ChangeNumber, parsedSplit.ChangeNumber);
            Assert.AreEqual(AlgorithmEnum.LegacyHash, parsedSplit.Algo);
            Assert.AreEqual(split.TrafficTypeName, parsedSplit.TrafficTypeName);
        }

        [TestMethod]
        public void ParseSuccessfullyWhenLegacyAlgorithm()
        {
            //Arrange
            Split split = new Split
            {
                Name = "test1",
                Seed = 2323,
                Status = "ACTIVE",
                Killed = false,
                DefaultTreatment = "off",
                ChangeNumber = 232323,
                Algo = 1,
                TrafficTypeName = "user",
                Conditions = new List<Condition>()
            };

            var parser = new InMemorySplitParser(null, null);

            //Act
            var parsedSplit = parser.Parse(split);

            //Assert
            Assert.IsNotNull(parsedSplit);
            Assert.AreEqual(split.Name, parsedSplit.Name);
            Assert.AreEqual(split.Seed, parsedSplit.Seed);
            Assert.AreEqual(split.Killed, parsedSplit.Killed);
            Assert.AreEqual(split.DefaultTreatment, parsedSplit.DefaultTreatment);
            Assert.AreEqual(split.ChangeNumber, parsedSplit.ChangeNumber);
            Assert.AreEqual(AlgorithmEnum.LegacyHash, parsedSplit.Algo);
            Assert.AreEqual(split.TrafficTypeName, parsedSplit.TrafficTypeName);
        }

        [TestMethod]
        public void ParseSuccessfullyWhenMurmurAlgorithm()
        {
            //Arrange
            Split split = new Split
            {
                Name = "test1",
                Seed = 2323,
                Status = "ACTIVE",
                Killed = false,
                DefaultTreatment = "off",
                ChangeNumber = 232323,
                Algo = 2,
                TrafficTypeName = "user",
                Conditions = new List<Condition>()
            };

            var parser = new InMemorySplitParser(null, null);

            //Act
            var parsedSplit = parser.Parse(split);

            //Assert
            Assert.IsNotNull(parsedSplit);
            Assert.AreEqual(split.Name, parsedSplit.Name);
            Assert.AreEqual(split.Seed, parsedSplit.Seed);
            Assert.AreEqual(split.Killed, parsedSplit.Killed);
            Assert.AreEqual(split.DefaultTreatment, parsedSplit.DefaultTreatment);
            Assert.AreEqual(split.ChangeNumber, parsedSplit.ChangeNumber);
            Assert.AreEqual(AlgorithmEnum.Murmur, parsedSplit.Algo);
            Assert.AreEqual(split.TrafficTypeName, parsedSplit.TrafficTypeName);
        }
    }
}
