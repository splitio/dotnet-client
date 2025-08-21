using Murmur;
using Splitio.Domain;
using Splitio.Services.Impressions.Interfaces;
using System;
using System.Text;

namespace Splitio.Services.Impressions.Classes
{
    public class ImpressionHasher : IImpressionHasher
    {
        public ulong Process(KeyImpression impression)
        {
            var key = $"{UnknowIfNull(impression.KeyName)}:{UnknowIfNull(impression.Feature)}:{UnknowIfNull(impression.Treatment)}:{UnknowIfNull(impression.Label)}:{ZeroIfNull(impression.ChangeNumber)}";

            return Hash(key, 0);
        }

        public static ulong Hash(string key, uint seed)
        {
            Murmur128 murmur128 = MurmurHash.Create128(seed: seed, preference: AlgorithmPreference.X64);
            byte[] keyToBytes = Encoding.ASCII.GetBytes(key);
            byte[] seedResult = murmur128.ComputeHash(keyToBytes);

            return BitConverter.ToUInt64(seedResult, 0);
        }

        private static string UnknowIfNull(string value)
        {
            return string.IsNullOrEmpty(value) ? "UNKNOWN" : value;
        }

        private static long ZeroIfNull(long? value)
        {
            return value == null ? 0 : value.Value;
        }
    }
}
