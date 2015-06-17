using ISVDemoUsage.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;

namespace ISVDemoUsage
{
    public static class AzureADGraphAPIUtil
    {
        public static string GetOrganizationDisplayName(string organizationId)
        {
            string displayName = null;

            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            try
            {
                // Aquire Access Token to call Azure AD Graph API
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                    ConfigurationManager.AppSettings["ida:Password"]);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                    string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), new ADALTokenCache(signedInUserUniqueName));

                AuthenticationResult result = authContext.AcquireTokenSilent(ConfigurationManager.AppSettings["ida:GraphAPIIdentifier"], credential,
                    new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                // Get a list of Organizations of which the user is a member
                string requestUrl = string.Format("{0}{1}/tenantDetails?api-version={2}", ConfigurationManager.AppSettings["ida:GraphAPIIdentifier"],
                    organizationId, ConfigurationManager.AppSettings["ida:GraphAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of Tenant Objects
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var organizationPropertiesResult = (Json.Decode(responseContent)).value;
                    if (organizationPropertiesResult != null && organizationPropertiesResult.Length > 0)
                    {
                        displayName = organizationPropertiesResult[0].displayName;
                        if (organizationPropertiesResult[0].verifiedDomains != null)
                            foreach (var verifiedDomain in organizationPropertiesResult[0].verifiedDomains)
                                if (verifiedDomain["default"])
                                    displayName += " (" + verifiedDomain.name + ")";
                    }
                }
            }
            catch { }

            return displayName;
        }
        public static string GetObjectIdOfServicePrincipalInOrganization(string organizationId, string applicationId )
        {
            string objectId = null;

            try
            {
                // Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                    ConfigurationManager.AppSettings["ida:Password"]);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId));
                AuthenticationResult result = authContext.AcquireToken(ConfigurationManager.AppSettings["ida:GraphAPIIdentifier"], credential);

                // Get a list of Organizations of which the user is a member
                string requestUrl = string.Format("{0}{1}/servicePrincipals?api-version={2}&$filter=appId eq '{3}'", 
                    ConfigurationManager.AppSettings["ida:GraphAPIIdentifier"], organizationId, 
                    ConfigurationManager.AppSettings["ida:GraphAPIVersion"], applicationId);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint should return JSON with one or none serviePrincipal object
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var servicePrincipalResult = (Json.Decode(responseContent)).value;
                    if (servicePrincipalResult != null && servicePrincipalResult.Length > 0)
                        objectId = servicePrincipalResult[0].objectId;
                }
            }
            catch { }

            return objectId;
        }
        public static string LookupDisplayNameOfAADObject(string organizationId, string objectId)
        {
            string objectDisplayName = null;

            string signedInUserUniqueName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

            // Aquire Access Token to call Azure AD Graph API
            ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                ConfigurationManager.AppSettings["ida:Password"]);
            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
            AuthenticationContext authContext = new AuthenticationContext(
                string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), new ADALTokenCache(signedInUserUniqueName));
            AuthenticationResult result = authContext.AcquireTokenSilent(ConfigurationManager.AppSettings["ida:GraphAPIIdentifier"], credential,
                new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

            HttpClient client = new HttpClient();

            string doQueryUrl = string.Format("{0}{1}/directoryObjects/{2}?api-version={3}",
                ConfigurationManager.AppSettings["ida:GraphAPIIdentifier"], organizationId,
                objectId, ConfigurationManager.AppSettings["ida:GraphAPIVersion"]);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, doQueryUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            HttpResponseMessage response = client.SendAsync(request).Result;

            if (response.IsSuccessStatusCode)
            {
                var responseContent = response.Content;
                string responseString = responseContent.ReadAsStringAsync().Result;
                var directoryObject = System.Web.Helpers.Json.Decode(responseString);
                if (directoryObject != null) objectDisplayName = string.Format("{0} ({1})", directoryObject.displayName, directoryObject.objectType);
            }

            return objectDisplayName;
        }
    }
}