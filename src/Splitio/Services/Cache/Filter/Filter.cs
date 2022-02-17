using Murmur;
using System;
using System.Security.Cryptography;

namespace Splitio.Services.Cache.Filter
{
    public abstract class Filter : IBloomFilter
    {        
        private int _hashes { get; }
        protected int _capacity { get; }

        public abstract bool Add(string data);
        public abstract bool Contains(string data);
        public abstract void Clear();

        public Filter(int expectedElements, double errorRate)
        {
            _capacity = BestM(expectedElements, errorRate);
            _hashes = BestK(expectedElements, _capacity);
        }

        protected int[] ComputeHash(byte[] data)
        {
            int m = _capacity;
            int k = _hashes;
            int[] positions = new int[k];

            long hash1 = GetHash(0, data);
            long hash2 = GetHash((uint)hash1, data);

            for (int i = 0; i < k; i++)
            {
                positions[i] = (int)((hash1 + i * hash2) % m);
            }

            return positions;
        }

        private long GetHash(uint seed, byte[] data)
        {
            HashAlgorithm hashAlgorithm = MurmurHash.Create32(seed);
            byte[] seedResult = hashAlgorithm.ComputeHash(data, 0, data.Length);

            return BitConverter.ToUInt32(seedResult, 0);
        }

        private static int BestM(long n, double p)
        {
            return (int)Math.Ceiling(-1 * (n * Math.Log(p)) / Math.Pow(Math.Log(2), 2));
        }

        private static int BestK(long n, long m)
        {
            return (int)Math.Ceiling((Math.Log(2) * m) / n);
        }
    }
}
