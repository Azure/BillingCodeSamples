using ISVDemoUsage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace ISVDemoUsage.Controllers
{
    public class HomeController : Controller
    {
        private DataAccess db = new DataAccess();

        public ActionResult Index()
        {
            HomeIndexViewModel model = null;

            if (ClaimsPrincipal.Current.Identity.IsAuthenticated)
            {
                model = new HomeIndexViewModel();
                model.UserOrganizations = new Dictionary<string,Organization>();
                model.UserSubscriptions = new Dictionary<string,Subscription>();
                model.UserCanManageAccessForSubscriptions = new List<string>();
                model.DisconnectedUserOrganizations = new List<string>();

                var orgnaizations = AzureResourceManagerUtil.GetUserOrganizations();
                foreach (Organization org in orgnaizations)
                {
                    model.UserOrganizations.Add(org.Id, org);
                    var subscriptions = AzureResourceManagerUtil.GetUserSubscriptions(org.Id);
                    
                    if (subscriptions != null)
                    {
                        foreach (var subscription in subscriptions)
                        {

                            Subscription s = db.Subscriptions.Find(subscription.Id);
                            if (s != null)
                            {
                                subscription.IsConnected = true;
                                subscription.ConnectedOn = s.ConnectedOn;
                                subscription.ConnectedBy = s.ConnectedBy;
                                subscription.AzureAccessNeedsToBeRepaired = AzureResourceManagerUtil.ServicePrincipalHasReadAccessToSubscription(subscription.Id, org.Id);
                                subscription.UsageString = AzureResourceManagerUtil.GetUsage(s.Id, org.Id);
                                //Deserialize the usage response into the usagePayload object
                                subscription.usagePayload = JsonConvert.DeserializeObject<UsagePayload>(subscription.UsageString);
                                List<UsageAggregate> UsageAggregateList = subscription.usagePayload.value;
                            }
                            else 
                            {
                                subscription.IsConnected = false;
                            }
                            model.UserSubscriptions.Add(subscription.Id, subscription);

                            if (AzureResourceManagerUtil.UserCanManageAccessForSubscription(subscription.Id, org.Id))
                                model.UserCanManageAccessForSubscriptions.Add(subscription.Id);
                        }
                    }
                    else
                        model.DisconnectedUserOrganizations.Add(org.Id);
                }
            }
            return View(model);
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}