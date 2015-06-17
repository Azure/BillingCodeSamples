using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ISVDemoUsage.Models
{
    public class InstanceData
    {
        public string resourceUri { get; set; }

        public IDictionary<string, string> tags { get; set; }

        public IDictionary<string, string> additionalInfo { get; set; }

        public string location { get; set; }

        public string partNumber { get; set; }

        public string orderNumber { get; set; }
    }
}