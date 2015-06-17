using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ISVDemoUsage.Models
{
    public class UsagePayload
    {
        public List<UsageAggregate> value { get; set; }
    }
}