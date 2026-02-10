using Splitio.Domain;
using Splitio.Services.Shared.Classes;

namespace Splitio.Redis.Domain
{
    public class RedisKeyImpression
    {
        public RedisKeyImpression(KeyImpression item, string SdkVersion, string MachineIp, string MachineName)
        {
            this.f = item.feature;
            k = item.keyName;
            this.t = item.treatment;
            this.m = item.time;
            this.c = item.changeNumber;
            this.r = item.label;
            this.b = item.bucketingKey;
            this.pt = item.previousTime;
            this.properties = item.properties;
            this.s = SdkVersion;
            this.i = MachineIp;
            this.n = MachineName;
        }

        public string ExportJson()
        {
            if (string.IsNullOrEmpty(this.properties))
            {
                return JsonConvertWrapper.SerializeObject(new
                {
                    m = new { s = this.s, i = this.i, n = this.n },
                    i = new { k = this.k, b = this.b, f = this.f, t = this.t, r = this.r, c = this.c, m = this.m, pt = this.pt }
                });
            } else {
                return JsonConvertWrapper.SerializeObject(new
                {
                    m = new { s = this.s, i = this.i, n = this.n },
                    i = new { k = this.k, b = this.b, f = this.f, t = this.t, r = this.r, c = this.c, m = this.m, pt = this.pt, properties = this.properties }
                });
            }
        }

        public string f { get; set; }
        public string k { get; set; }
        public string t { get; set; }
        public long m { get; set; }
        public long? c { get; set; }
        public string r { get; set; }
        public string b { get; set; }
        public long? pt { get; set; }
        public string properties { get; set; }

        public string s { get; set; }
        public string i {  get; set; }
        public string n { get; set; }

    }
}
