using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Impressions.Classes;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class ImpressionsObserverTests
    {
        [TestMethod]
        public void TestAndSet()
        {
            var impressionsObserver = new ImpressionsObserver(new ImpressionHasher());

            var impression = new KeyImpression
            {
                KeyName = "matching_key",
                BucketingKey = "bucketing_key",
                Feature = "split_name",
                Treatment = "treatment",
                Label = "default label",
                ChangeNumber = 1533177602748,
                Time = 1478113516022
            };

            var impression2 = new KeyImpression
            {
                KeyName = "matching_key_2",
                BucketingKey = "bucketing_key_2",
                Feature = "split_name_2",
                Treatment = "treatment_2",
                Label = "default label_2",
                ChangeNumber = 1533177602748,
                Time = 1478113516022
            };

            var result = impressionsObserver.TestAndSet(impression);
            Assert.IsNull(result);

            // Should return previous time
            impression.Time = 1478113516500;
            result = impressionsObserver.TestAndSet(impression);
            Assert.AreEqual(1478113516022, result);

            // Should return the new impression.time
            result = impressionsObserver.TestAndSet(impression);
            Assert.AreEqual(1478113516500, result);

            // When impression.time < previous should return the min.
            impression.Time = 1478113516001;
            result = impressionsObserver.TestAndSet(impression);
            Assert.AreEqual(1478113516001, result);

            // Should return null because is another impression
            result = impressionsObserver.TestAndSet(impression2);
            Assert.IsNull(result);

            // Should return previous time
            impression2.Time = 1478113516500;
            result = impressionsObserver.TestAndSet(impression2);
            Assert.AreEqual(1478113516022, result);

            // Should return null because the impression is null
            result = impressionsObserver.TestAndSet(null);
            Assert.IsNull(result);
        }
    }
}
