using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ISVDemoUsage.Models
{
    public class HomeIndexViewModel
    {
        public Dictionary<string, Organization> UserOrganizations { get; set; }
        public Dictionary<string, Subscription> UserSubscriptions { get; set; }
        public List<string> UserCanManageAccessForSubscriptions { get; set; }
        public List<string> DisconnectedUserOrganizations { get; set; }
    }
}