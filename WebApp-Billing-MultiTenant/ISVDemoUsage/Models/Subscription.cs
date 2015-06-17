using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ISVDemoUsage.Models
{
    public class Subscription
    {
        public string Id { get; set; }
        [NotMapped]
        public string DisplayName { get; set; }
        [NotMapped]
        public string OrganizationId { get; set; }
        [NotMapped]
        public bool IsConnected { get; set; }
        public DateTime ConnectedOn { get; set; }
        public string ConnectedBy { get; set; }
        [NotMapped]
        public UsagePayload usagePayload { get; set; }
        public string UsageString { get; set; }
        public bool AzureAccessNeedsToBeRepaired { get; set; }
    }
}