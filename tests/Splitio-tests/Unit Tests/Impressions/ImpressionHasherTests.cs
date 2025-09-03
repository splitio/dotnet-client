using Microsoft.VisualStudio.TestTools.UnitTesting;
using Splitio.Domain;
using Splitio.Services.Impressions.Classes;
using System;
using System.IO;
using System.Linq;

namespace Splitio_Tests.Unit_Tests.Impressions
{
    [TestClass]
    public class ImpressionHasherTests
    {
        private readonly string rootFilePath;

        public ImpressionHasherTests()
        {
            // This line is to clean the warnings.
            rootFilePath = string.Empty;

#if NET_LATEST
            rootFilePath = @"Resources\";
#endif
        }

        [TestMethod]
        public void Works()
        {
            var impressionHasher = new ImpressionHasher();

            var impression = new KeyImpression
            {
                Feature = "someFeature",
                KeyName = "someKeyName",
                Treatment = "someTreatment",
                ChangeNumber = 3245463,
                Label = "someLabel"
            };

            var impression2 = new KeyImpression
            {
                Feature = "someFeature",
                KeyName = "someKeyName",
                Treatment = "otherTreatment",
                ChangeNumber = 3245463,
                Label = "someLabel"
            };

            var result = impressionHasher.Process(impression);
            var result2 = impressionHasher.Process(impression2);
            Assert.AreNotEqual(result, result2);

            impression2.KeyName = "otherKeyName";
            var result3 = impressionHasher.Process(impression2);
            Assert.AreNotEqual(result2, result3);

            impression2.Feature = "otherFeature";
            var result4 = impressionHasher.Process(impression2);
            Assert.AreNotEqual(result3, result4);

            impression2.Treatment = "treatment";
            var result5 = impressionHasher.Process(impression2);
            Assert.AreNotEqual(result4, result5);

            impression2.Label = "otherLabel";
            var result6 = impressionHasher.Process(impression2);
            Assert.AreNotEqual(result5, result6);

            impression2.ChangeNumber = 888755;
            var result7 = impressionHasher.Process(impression2);
            Assert.AreNotEqual(result6, result7);
        }

        [TestMethod]
        public void DoesNotCrash()
        {
            var impressionHasher = new ImpressionHasher();

            var impression = new KeyImpression
            {
                Feature = null,
                KeyName = "someKeyName",
                Treatment = "someTreatment",
                ChangeNumber = 3245463,
                Label = "someLabel"
            };

            Assert.IsNotNull(impressionHasher.Process(impression));

            impression.KeyName = null;
            Assert.IsNotNull(impressionHasher.Process(impression));

            impression.ChangeNumber = null;
            Assert.IsNotNull(impressionHasher.Process(impression));

            impression.Label = null;
            Assert.IsNotNull(impressionHasher.Process(impression));

            impression.Treatment = null;
            Assert.IsNotNull(impressionHasher.Process(impression));
        }

        [DeploymentItem(@"Resources\murmur3-64-128.csv")]
        [TestMethod]
        public void TestingMurmur128WithCsv()
        {
            var fileContent = File.ReadAllText($"{rootFilePath}murmur3-64-128.csv");
            var contents = fileContent.Split(new string[] { "\n" }, StringSplitOptions.None);
            var csv = contents.Select(x => x.Split(',')).ToArray();

            foreach (var item in csv)
            {
                if (item.Length != 3)
                    continue;

                var key = item[0];
                var seed = uint.Parse(item[1]);
                var expected = ulong.Parse(item[2]);

                Assert.AreEqual(expected, ImpressionHasher.Hash(key, seed));
            }
        }
    }
}
