using System.Collections;
using System.Text;

namespace Splitio.Services.Cache.Filter
{
    public class BloomFilter : Filter
    {
        private readonly object sync = new object();
        private readonly BitArray _hashBits;        

        public BloomFilter(int expectedElements, double errorRate)
            : base (expectedElements, errorRate)
        {
            _hashBits = new BitArray(_capacity);
        }

        public override bool Add(string data)
        {
            byte[] element = Encoding.UTF8.GetBytes(data);
            bool added = false;
            int[] positions = ComputeHash(element);

            lock (sync)
            {
                foreach (int position in positions)
                {
                    if (!_hashBits.Get(position))
                    {
                        added = true;
                        _hashBits.Set(position, true);
                    }
                }
            }

            return added;
        }        

        public override bool Contains(string data)
        {
            byte[] element = Encoding.UTF8.GetBytes(data);
            int[] positions = ComputeHash(element);

            lock (sync)
            {
                foreach (int position in positions)
                {
                    if (!_hashBits.Get(position))
                        return false;
                }
            }

            return true;
        }

        public override void Clear()
        {
            lock (sync)
            {
                _hashBits.SetAll(false);
            }
        }
    }
}
