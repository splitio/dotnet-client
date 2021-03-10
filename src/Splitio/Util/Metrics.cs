using System;

namespace Splitio.Util
{
    public class Metrics
    {
        private static readonly long MaxLatency = 7481828;

        private static readonly long[] LatencyBuckets = {
            1000,    1500,    2250,   3375,    5063,
            7594,    11391,   17086,  25629,   38443,
            57665,   86498,   129746, 194620,  291929,
            437894,  656841,  985261, 1477892, 2216838,
            3325257, 4987885, 7481828
        };

        public static int Bucket(long latency)
        {
            latency = latency * 1000; // Converto to microseconds

            if (latency > MaxLatency)
            {
                return LatencyBuckets.Length - 1;
            }

            int index = Array.BinarySearch(LatencyBuckets, latency);

            if (index < 0)
            {
                //When index is negative, do bitwise negation
                index = ~index;
            }

            return index;
        }
    }
}
