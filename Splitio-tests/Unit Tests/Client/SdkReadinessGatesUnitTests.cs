using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Services.Client.Classes;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Splitio_Tests.Unit_Tests.Client
{
    [TestClass]
    public class InMemoryReadinessGatesCacheUnitTests
    {
        [TestMethod]
        public void TestConcurrency()
        {
            //Arrange
            var gates = new InMemoryReadinessGatesCache();
            var results = new List<bool>();
            var token = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                Task.Factory.StartNew(() => gates.RegisterSegment($"segment-1"), token.Token);
                Thread.Sleep(5);
                Task.Factory.StartNew(() => gates.RegisterSegment($"segment-2"), token.Token);
                Thread.Sleep(5);
                Task.Factory.StartNew(() => gates.RegisterSegment($"segment-3"), token.Token);
                Thread.Sleep(5);
                Task.Factory.StartNew(() => gates.RegisterSegment($"segment-4"), token.Token);
                Thread.Sleep(5);
                Task.Factory.StartNew(() => gates.RegisterSegment($"segment-5"), token.Token);
                Thread.Sleep(5);
                Task.Factory.StartNew(() => gates.RegisterSegment($"segment-6"), token.Token);
                Thread.Sleep(5);
                Task.Factory.StartNew(() => gates.RegisterSegment($"segment-7"), token.Token);
                Thread.Sleep(5);
            }, token.Token);

            //Act
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(10);
                results.Add(gates.AreSegmentsReady(1000));
            }

            token.Cancel();
            token.Dispose();
        }

        [TestMethod]
        public void IsSDKReadyShouldReturnFalseIfSplitsAreNotReady()
        {
            //Arrange
            var gates = new InMemoryReadinessGatesCache();

            //Act
            var result = gates.IsSDKReady(0);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsSDKReadyShouldReturnFalseIfAnySegmentIsNotReady()
        {
            //Arrange
            var gates = new InMemoryReadinessGatesCache();
            gates.RegisterSegment("any");
            gates.SplitsAreReady();

            //Act
            var result = gates.IsSDKReady(0);

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsSDKReadyShouldReturnTrueIfSplitsAndSegmentsAreReady()
        {
            //Arrange
            var gates = new InMemoryReadinessGatesCache();
            gates.RegisterSegment("any");
            gates.RegisterSegment("other");
            gates.SplitsAreReady();
            gates.SegmentIsReady("other");
            gates.SegmentIsReady("any");

            //Act
            var result = gates.IsSDKReady(0);

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void RegisterSegmentShouldReturnFalseIfSplitsAreReady()
        {
            //Arrange
            var gates = new InMemoryReadinessGatesCache();
            gates.SplitsAreReady();

            //Act
            var result = gates.RegisterSegment("any");

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RegisterSegmentShouldReturnFalseIfSegmentNameEmpty()
        {
            //Arrange
            var gates = new InMemoryReadinessGatesCache();

            //Act
            var result = gates.RegisterSegment("");

            //Assert
            Assert.IsFalse(result);
        }
    }
}
