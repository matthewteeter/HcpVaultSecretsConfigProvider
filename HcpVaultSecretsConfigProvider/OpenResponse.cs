using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HcpVaultSecretsConfigProvider
{

    public class OpenResponse
    {
        public Secret[] secrets { get; set; }
    }

    public class Secret
    {
        public string name { get; set; }
        public Version version { get; set; }
        public DateTime created_at { get; set; }
        public string latest_version { get; set; }
        public Created_By1 created_by { get; set; }
        public Sync_Status sync_status { get; set; }
    }

    public class Version
    {
        public string version { get; set; }
        public string type { get; set; }
        public DateTime created_at { get; set; }
        public string value { get; set; }
        public Created_By created_by { get; set; }
    }

    public class Created_By
    {
        public string name { get; set; }
        public string type { get; set; }
        public string email { get; set; }
    }

    public class Created_By1
    {
        public string name { get; set; }
        public string type { get; set; }
        public string email { get; set; }
    }

    public class Sync_Status
    {
    }

}
