using ISVDemoUsage.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;

namespace ISVDemoUsage
{
    public static class AzureResourceManagerUtil
    {
        public static List<Organization> GetUserOrganizations()
        {
            List<Organization> organizations = new List<Organization>();

            string tenantId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];
           
            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                    ConfigurationManager.AppSettings["ida:Password"]);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                    string.Format(ConfigurationManager.AppSettings["ida:Authority"], tenantId), new ADALTokenCache(signedInUserUniqueName));

                var items = authContext.TokenCache.ReadItems().ToList();


                AuthenticationResult result = authContext.AcquireTokenSilent(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                    new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                 items = authContext.TokenCache.ReadItems().ToList();


                // Get a list of Organizations of which the user is a member            
                string requestUrl = string.Format("{0}/tenants?api-version={1}", ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of Tenant Objects
                // id                                            tenantId
                // --                                            --------
                // /tenants/7fe877e6-a150-4992-bbfe-f517e304dfa0 7fe877e6-a150-4992-bbfe-f517e304dfa0
                // /tenants/62e173e9-301e-423e-bcd4-29121ec1aa24 62e173e9-301e-423e-bcd4-29121ec1aa24

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var organizationsResult = (Json.Decode(responseContent)).value;

                    foreach (var organization in organizationsResult)
                        organizations.Add(new Organization()
                        {
                            Id = organization.tenantId,
                            //DisplayName = AzureADGraphAPIUtil.GetOrganizationDisplayName(organization.tenantId),
                            objectIdOfISVDemoUsageServicePrincipal =
                                AzureADGraphAPIUtil.GetObjectIdOfServicePrincipalInOrganization(organization.tenantId, ConfigurationManager.AppSettings["ida:ClientID"])
                        });
                }
            }
            catch
            {
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                       ConfigurationManager.AppSettings["ida:Password"]);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                    string.Format(ConfigurationManager.AppSettings["ida:Authority"], tenantId), new ADALTokenCache(signedInUserUniqueName));

                var items = authContext.TokenCache.ReadItems().ToList();


                AuthenticationResult result =authContext.AcquireToken(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential);


                    //(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                    //new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                items = authContext.TokenCache.ReadItems().ToList();


                // Get a list of Organizations of which the user is a member            
                string requestUrl = string.Format("{0}/tenants?api-version={1}", ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of Tenant Objects
                // id                                            tenantId
                // --                                            --------
                // /tenants/7fe877e6-a150-4992-bbfe-f517e304dfa0 7fe877e6-a150-4992-bbfe-f517e304dfa0
                // /tenants/62e173e9-301e-423e-bcd4-29121ec1aa24 62e173e9-301e-423e-bcd4-29121ec1aa24

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var organizationsResult = (Json.Decode(responseContent)).value;

                    foreach (var organization in organizationsResult)
                        organizations.Add(new Organization()
                        {
                            Id = organization.tenantId,
                            //DisplayName = AzureADGraphAPIUtil.GetOrganizationDisplayName(organization.tenantId),
                            objectIdOfISVDemoUsageServicePrincipal =
                                AzureADGraphAPIUtil.GetObjectIdOfServicePrincipalInOrganization(organization.tenantId, ConfigurationManager.AppSettings["ida:ClientID"])
                        });
                }
            }
            return organizations;
        }
        public static List<Subscription> GetUserSubscriptions(string organizationId)
        {
            List<Subscription> subscriptions = null;

            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                    ConfigurationManager.AppSettings["ida:Password"]);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                    string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId),
                    new ADALTokenCache(signedInUserUniqueName));
                AuthenticationResult result =
                    authContext.AcquireTokenSilent(
                        ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                        new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                subscriptions = new List<Subscription>();

                // Get subscriptions to which the user has some kind of access
                string requestUrl = string.Format("{0}/subscriptions?api-version={1}",
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var subscriptionsResult = (Json.Decode(responseContent)).value;

                    foreach (var subscription in subscriptionsResult)
                        subscriptions.Add(new Subscription()
                        {
                            Id = subscription.subscriptionId,
                            DisplayName = subscription.displayName,
                            OrganizationId = organizationId
                        });
                }
            }
            catch
            {

                //ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                //   ConfigurationManager.AppSettings["ida:Password"]);
                //// initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                //AuthenticationContext authContext = new AuthenticationContext(
                //    string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), new ADALTokenCache(signedInUserUniqueName));
                //string resource = ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"];


                //AuthenticationResult result = authContext.AcquireToken(resource, credential.ClientId,
                //    new UserCredential(signedInUserUniqueName));

                ////authContext.AcquireToken(resource,userAssertion:)
                    
                //    //new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId)));
                    
                //    //AcquireTokenSilent(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                //    //new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                //subscriptions = new List<Subscription>();

                //// Get subscriptions to which the user has some kind of access
                //string requestUrl = string.Format("{0}/subscriptions?api-version={1}", ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                //    ConfigurationManager.AppSettings["ida:AzureResourceManagerAPIVersion"]);

                //// Make the GET request
                //HttpClient client = new HttpClient();
                //HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                //HttpResponseMessage response = client.SendAsync(request).Result;

                //if (response.IsSuccessStatusCode)
                //{
                //    string responseContent = response.Content.ReadAsStringAsync().Result;
                //    var subscriptionsResult = (Json.Decode(responseContent)).value;

                //    foreach (var subscription in subscriptionsResult)
                //        subscriptions.Add(new Subscription()
                //        {
                //            Id = subscription.subscriptionId,
                //            DisplayName = subscription.displayName,
                //            OrganizationId = organizationId
                //        });
                //}
            }

            return subscriptions;
        }
        public static string GetUsage(string subscriptionId, string organizationId)
        {
            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];
            string UsageResponse = "";
            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                    ConfigurationManager.AppSettings["ida:Password"]);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                    string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), new ADALTokenCache(signedInUserUniqueName));
                AuthenticationResult result = authContext.AcquireTokenSilent(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                    new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));
                //Making a call to the Azure Usage API for a set time frame with the input AzureSubID
                string requesturl = String.Format("https://management.azure.com/subscriptions/{0}/providers/Microsoft.Commerce/UsageAggregates?api-version=2015-06-01-preview&reportedstartTime=2015-05-15+00%3a00%3a00Z&reportedEndTime=2015-05-16+00%3a00%3a00Z", subscriptionId);
                
                //Crafting the HTTP call
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requesturl);
                request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + result.AccessToken);
                request.ContentType = "application/json";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Console.WriteLine(response.StatusDescription);
                Stream receiveStream = response.GetResponseStream();

                // Pipes the stream to a higher level stream reader with the required encoding format. 
                StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
                UsageResponse = readStream.ReadToEnd();
              }
            catch { }

            return UsageResponse;
        }
        public static bool UserCanManageAccessForSubscription(string subscriptionId, string organizationId)
        {
            bool ret = false;

            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                    ConfigurationManager.AppSettings["ida:Password"]);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                    string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), new ADALTokenCache(signedInUserUniqueName));
                AuthenticationResult result = authContext.AcquireTokenSilent(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                    new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));


                // Get permissions of the user on the subscription
                string requestUrl = string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId, ConfigurationManager.AppSettings["ida:ARMAuthorizationPermissionsAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of Actions and NotActions
                // actions  notActions
                // -------  ----------
                // {*}      {Microsoft.Authorization/*/Write, Microsoft.Authorization/*/Delete}
                // {*/read} {}

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var permissionsResult = (Json.Decode(responseContent)).value;

                    foreach (var permissions in permissionsResult)
                    {
                        bool permissionMatch = false;
                        foreach (string action in permissions.actions)
                        {
                            var actionPattern = "^" + Regex.Escape(action.ToLower()).Replace("\\*", ".*") + "$";
                            permissionMatch = Regex.IsMatch("microsoft.authorization/roleassignments/write", actionPattern);
                            if (permissionMatch) break;
                        }
                        // if one of the actions match, check that the NotActions don't
                        if (permissionMatch)
                        {
                            foreach (string notAction in permissions.notActions)
                            {
                                var notActionPattern = "^" + Regex.Escape(notAction.ToLower()).Replace("\\*", ".*") + "$";
                                if (Regex.IsMatch("microsoft.authorization/roleassignments/write", notActionPattern))
                                    permissionMatch = false;
                                if (!permissionMatch) break;
                            }
                        }
                        if (permissionMatch)
                        {
                            ret = true;
                            break;
                        }
                    }
                }
            }
            catch { }

            return ret;
        }
        public static bool ServicePrincipalHasReadAccessToSubscription(string subscriptionId, string organizationId)
        {
            bool ret = false;

            try
            {
                // Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                    ConfigurationManager.AppSettings["ida:Password"]);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId));
                AuthenticationResult result = authContext.AcquireToken(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential);


                // Get permissions of the app on the subscription
                string requestUrl = string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId, ConfigurationManager.AppSettings["ida:ARMAuthorizationPermissionsAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of Actions and NotActions
                // actions  notActions
                // -------  ----------
                // {*}      {Microsoft.Authorization/*/Write, Microsoft.Authorization/*/Delete}
                // {*/read} {}

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var permissionsResult = (Json.Decode(responseContent)).value;

                    foreach (var permissions in permissionsResult)
                    {
                        bool permissionMatch = false;
                        foreach (string action in permissions.actions)
                            if (action.Equals("*/read", StringComparison.CurrentCultureIgnoreCase) || action.Equals("*", StringComparison.CurrentCultureIgnoreCase))
                            {
                                permissionMatch = true;
                                break;
                            }
                        // if one of the actions match, check that the NotActions don't
                        if (permissionMatch)
                        {
                            foreach (string notAction in permissions.notActions)
                                if (notAction.Equals("*", StringComparison.CurrentCultureIgnoreCase) || notAction.EndsWith("/read", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    permissionMatch = false;
                                    break;
                                }
                        }
                        if (permissionMatch)
                        {
                            ret = true;
                            break;
                        }
                    }
                }
            }
            catch { }

            return ret;
        }
        public static void GrantRoleToServicePrincipalOnSubscription(string objectId, string subscriptionId, string organizationId)
        {
            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                    ConfigurationManager.AppSettings["ida:Password"]);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                    string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), new ADALTokenCache(signedInUserUniqueName));
                AuthenticationResult result = authContext.AcquireTokenSilent(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                    new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));


                // Create role assignment for application on the subscription
                string roleAssignmentId = Guid.NewGuid().ToString();
                string roleDefinitionId = GetRoleId(ConfigurationManager.AppSettings["ida:RequiredARMRoleOnSubscription"], subscriptionId, organizationId);

                string requestUrl = string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/roleassignments/{2}?api-version={3}",
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId, roleAssignmentId,
                    ConfigurationManager.AppSettings["ida:ARMAuthorizationRoleAssignmentsAPIVersion"]);

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                StringContent content = new StringContent("{\"properties\": {\"roleDefinitionId\":\"" + roleDefinitionId + "\",\"principalId\":\"" + objectId + "\"}}");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;
                HttpResponseMessage response = client.SendAsync(request).Result;
            }
            catch { }
        }
        public static void RevokeRoleFromServicePrincipalOnSubscription(string objectId, string subscriptionId, string organizationId)
        {
            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                    ConfigurationManager.AppSettings["ida:Password"]);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                    string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), new ADALTokenCache(signedInUserUniqueName));
                AuthenticationResult result = authContext.AcquireTokenSilent(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                    new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));


                // Get rolesAssignments to application on the subscription
                string requestUrl = string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/roleassignments?api-version={2}&$filter=principalId eq '{3}'",
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                    ConfigurationManager.AppSettings["ida:ARMAuthorizationRoleAssignmentsAPIVersion"], objectId);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of role assignments
                // properties                                  id                                          type                                        name
                // ----------                                  --                                          ----                                        ----
                // @{roleDefinitionId=/subscriptions/e91d47... /subscriptions/e91d47c4-76f3-4271-a796-2... Microsoft.Authorization/roleAssignments     9db2cdc1-2971-42fe-bd21-c7c4ead4b1b8

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var roleAssignmentsResult = (Json.Decode(responseContent)).value;

                    //remove all role assignments
                    foreach (var roleAssignment in roleAssignmentsResult)
                    {
                        requestUrl = string.Format("{0}{1}?api-version={2}",
                            ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], roleAssignment.id,
                            ConfigurationManager.AppSettings["ida:ARMAuthorizationRoleAssignmentsAPIVersion"]);
                        request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                        response = client.SendAsync(request).Result;
                    }
                }
            }
            catch { }
        }
        public static string GetRoleId(string roleName, string subscriptionId, string organizationId)
        {
            string roleId = null;

            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                    ConfigurationManager.AppSettings["ida:Password"]);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                    string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), new ADALTokenCache(signedInUserUniqueName));
                AuthenticationResult result = authContext.AcquireTokenSilent(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                    new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                // Get subscriptions to which the user has some kind of access
                string requestUrl = string.Format("{0}/subscriptions/{1}/providers/Microsoft.Authorization/roleDefinitions?api-version={2}", 
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                    ConfigurationManager.AppSettings["ida:ARMAuthorizationRoleDefinitionsAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of roleDefinition Objects
                // properties                                  id                                          type                                        name
                // ----------                                  --                                          ----                                        ----
                // @{roleName=Contributor; type=BuiltInRole... /subscriptions/e91d47c4-76f3-4271-a796-2... Microsoft.Authorization/roleDefinitions     b24988ac-6180-42a0-ab88-20f7382dd24c
                // @{roleName=Owner; type=BuiltInRole; desc... /subscriptions/e91d47c4-76f3-4271-a796-2... Microsoft.Authorization/roleDefinitions     8e3af657-a8ff-443c-a75c-2fe8c4bcb635
                // @{roleName=Reader; type=BuiltInRole; des... /subscriptions/e91d47c4-76f3-4271-a796-2... Microsoft.Authorization/roleDefinitions     acdd72a7-3385-48ef-bd42-f606fba81ae7
                // ...

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var roleDefinitionsResult = (Json.Decode(responseContent)).value;

                    foreach (var roleDefinition in roleDefinitionsResult)
                        if ((roleDefinition.properties.roleName as string).Equals(roleName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            roleId = roleDefinition.id;
                            break;
                        }
                }
            }
            catch { }

            return roleId;
        }
    }
}