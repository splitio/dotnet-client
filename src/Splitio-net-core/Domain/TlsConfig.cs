using System.Collections.Generic;

namespace Splitio.Domain
{
    public class TlsConfig
    {
        public bool Ssl { get; set; }
        public List<string> SslCaCertificates { get; set; }
        public string SslClientCertificate { get; set; }
        public bool InsecureSkipVerify { get; set; }
    }
}
