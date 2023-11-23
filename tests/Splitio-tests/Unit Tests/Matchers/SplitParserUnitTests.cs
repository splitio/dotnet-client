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
                name = "test1",
                seed = 2323,
                status = "ACTIVE",
                killed = false,
                defaultTreatment = "off",
                changeNumber = 232323,
                trafficTypeName = "user",
                conditions = new List<ConditionDefinition>()
            };

            var parser = new InMemorySplitParser(null, null);

            //Act
            var parsedSplit = parser.Parse(split);

            //Assert
            Assert.IsNotNull(parsedSplit);
            Assert.AreEqual(split.name, parsedSplit.name);
            Assert.AreEqual(split.seed, parsedSplit.seed);
            Assert.AreEqual(split.killed, parsedSplit.killed);
            Assert.AreEqual(split.defaultTreatment, parsedSplit.defaultTreatment);
            Assert.AreEqual(split.changeNumber, parsedSplit.changeNumber);
            Assert.AreEqual(AlgorithmEnum.LegacyHash, parsedSplit.algo);
            Assert.AreEqual(split.trafficTypeName, parsedSplit.trafficTypeName);
        }

        [TestMethod]
        public void ParseSuccessfullyWhenLegacyAlgorithm()
        {
            //Arrange
            Split split = new Split
            {
                name = "test1",
                seed = 2323,
                status = "ACTIVE",
                killed = false,
                defaultTreatment = "off",
                changeNumber = 232323,
                algo = 1,
                trafficTypeName = "user",
                conditions = new List<ConditionDefinition>()
            };

            var parser = new InMemorySplitParser(null, null);

            //Act
            var parsedSplit = parser.Parse(split);

            //Assert
            Assert.IsNotNull(parsedSplit);
            Assert.AreEqual(split.name, parsedSplit.name);
            Assert.AreEqual(split.seed, parsedSplit.seed);
            Assert.AreEqual(split.killed, parsedSplit.killed);
            Assert.AreEqual(split.defaultTreatment, parsedSplit.defaultTreatment);
            Assert.AreEqual(split.changeNumber, parsedSplit.changeNumber);
            Assert.AreEqual(AlgorithmEnum.LegacyHash, parsedSplit.algo);
            Assert.AreEqual(split.trafficTypeName, parsedSplit.trafficTypeName);
        }

        [TestMethod]
        public void ParseSuccessfullyWhenMurmurAlgorithm()
        {
            //Arrange
            Split split = new Split
            {
                name = "test1",
                seed = 2323,
                status = "ACTIVE",
                killed = false,
                defaultTreatment = "off",
                changeNumber = 232323,
                algo = 2,
                trafficTypeName = "user",
                conditions = new List<ConditionDefinition>()
            };

            var parser = new InMemorySplitParser(null, null);

            //Act
            var parsedSplit = parser.Parse(split);

            //Assert
            Assert.IsNotNull(parsedSplit);
            Assert.AreEqual(split.name, parsedSplit.name);
            Assert.AreEqual(split.seed, parsedSplit.seed);
            Assert.AreEqual(split.killed, parsedSplit.killed);
            Assert.AreEqual(split.defaultTreatment, parsedSplit.defaultTreatment);
            Assert.AreEqual(split.changeNumber, parsedSplit.changeNumber);
            Assert.AreEqual(AlgorithmEnum.Murmur, parsedSplit.algo);
            Assert.AreEqual(split.trafficTypeName, parsedSplit.trafficTypeName);
        }
    }
}
