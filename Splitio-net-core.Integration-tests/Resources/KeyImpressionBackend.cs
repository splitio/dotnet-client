﻿using System.Collections.Generic;

namespace Splitio_net_core.Integration_tests.Resources
{
    public class KeyImpressionBackend
    {
        public string F { get; set; }
        public List<ImpressionData> I { get; set; }
    }

    public class ImpressionData
    {
        public string K { get; set; }
        public string B { get; set; }
        public string T { get; set; }
        public string R { get; set; }
        public long? C { get; set; }
        public long? M { get; set; }
    }

    public class KeyImpressionRedis
    {
        public MachineRedis M { get; set; }
        public InfoRedis I { get; set; }
    }

    public class MachineRedis
    {
        public string S { get; set; }
        public string I { get; set; }
        public string N { get; set; }
    }

    public class InfoRedis
    {
        public string K { get; set; }
        public string B { get; set; }
        public string F { get; set; }
        public string T { get; set; }
        public string R { get; set; }
        public long? C { get; set; }
        public long? M { get; set; }
    }

    public class ImpressionCount
    {
        public List<ImpressionCountData> Pf { get; set; }
    }

    public class ImpressionCountData
    {
        public string F { get; set; }
        public long M { get; set; }
        public int Rc { get; set; }
    }
}
