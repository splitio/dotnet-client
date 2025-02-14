using Newtonsoft.Json;

namespace Splitio.Domain
{
    public class WrappedKeyImpression
    {
        public WrappedKeyImpression() { }

        public WrappedKeyImpression(KeyImpression keyImpression, bool impressionsDisabled)
        {
            this.keyImpression = keyImpression;
            this.impressionsDisabled = impressionsDisabled;
           
        }

        public KeyImpression keyImpression { get; set; }
        public bool impressionsDisabled { get; set; }
    }
}
