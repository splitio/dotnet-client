using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Splitio.Domain
{
    public class TlsConfig
    {
        /// <summary>
        /// From StackExchange.Redis: Indicates whether the connection should be encrypted.
        /// </summary>
        public bool Ssl { get; set; }
        /// <summary>
        /// From StackExchange.Redis: A RemoteCertificateValidationCallback delegate responsible for validating the certificate supplied by the remote party; note that this cannot be specified in the configuration-string.
        /// </summary>
        public Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> CertificateValidationFunc { get; set; }
        /// <summary>
        /// From StackExchange.Redis: A LocalCertificateSelectionCallback delegate responsible for selecting the certificate used for authentication; note that this cannot be specified in the configuration-string.
        /// </summary>
        public Func<object, string, X509CertificateCollection, X509Certificate, string[], X509Certificate2> CertificateSelectionFunc { get; set; }

        public TlsConfig(bool ssl)
        {
            Ssl = ssl;
            CertificateValidationFunc = null;
            CertificateSelectionFunc = null;
        }
    }
}
