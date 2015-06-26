namespace ARMAPI_Test
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Net;
    using System.Text;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Newtonsoft.Json;
    using RestSharp.Contrib;
    
    class Program
    {
        #error Please update the appSettings section in app.config, then remove this statement
        
        //This is a sample console application that shows you how to grab a token from AAD for the current user of the app, and then get usage data for the customer with that token.
        //The same caveat remains, that the current user of the app needs to be part of either the Owner, Reader or Contributor role for the requested AzureSubID.
        static void Main(string[] args)
        {
                      
            //Get the AAD token to get authorized to make the call to the Usage API
            string token = GetOAuthTokenFromAAD();

            /*Setup API call to Usage API
             Callouts:
             * See the App.config file for all AppSettings key/value pairs
             * You can get a list of offer numbers from this URL: http://azure.microsoft.com/en-us/support/legal/offer-details/
             * See the Azure Usage API specification for more details on the query parameters for this API.
             * The Usage Service/API is currently in preview; please use 2016-06-01-preview for api-version
            */

            // Build up the HttpWebRequest
            //string requestURLold = String.Format("{0}/{1}/{2}/{3}",
            //           ConfigurationManager.AppSettings["ARMBillingServiceURL"],
            //           "subscriptions",
            //           ConfigurationManager.AppSettings["SubscriptionID"],
            //           "providers/Microsoft.Commerce/UsageAggregates?" +
            //           "api-version=2015-06-01-preview&" +
            //           "reportedstartTime=2015-03-01+00%3a00%3a00Z&" +
            //           "reportedEndTime=2015-05-18+00%3a00%3a00Z");

            string requestURL = GetReportingUri(
                billingServiceURL: ConfigurationManager.AppSettings["ARMBillingServiceURL"],
                subscriptionId: ConfigurationManager.AppSettings["SubscriptionID"],
                from: new DateTime(year: 2015, month: 03, day: 01, hour: 0, minute:0, second:0, kind: DateTimeKind.Utc),
                to: new DateTime(year: 2015, month: 05, day: 18, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Utc),
                granularity: AggregationGranularity.Hourly, 
                showDetails: true);

            // Call the Usage API, dump the output to the console window
            try
            {
                int count = 0;
                string nextLink = requestURL;
                while (!string.IsNullOrEmpty(nextLink))
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(nextLink);

                    // Add the OAuth Authorization header, and Content Type header
                    request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + token);
                    request.ContentType = "application/json";

                    // Call the REST endpoint
                    Console.WriteLine("Calling Usage service...");
                    string usageResponse = null;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        Console.WriteLine(String.Format("Usage service response status: {0}", response.StatusDescription));
                        using (Stream receiveStream = response.GetResponseStream())
                        {
                            // Pipes the stream to a higher level stream reader with the required encoding format. 
                            using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                            {
                                usageResponse = readStream.ReadToEnd();
                            }
                        }
                    }

                    var filename = string.Format("usage-{0}.json", count++);
                    File.WriteAllText(filename, usageResponse);
                    UsagePayload payload = JsonConvert.DeserializeObject<UsagePayload>(usageResponse);
                    nextLink = payload.nextLink;

                    //Console.WriteLine("Usage stream received.  Press ENTER to continue with raw output.");
                    //Console.ReadLine();
                    //Console.WriteLine(usageResponse);
                    //Console.WriteLine("Raw output complete.  Press ENTER to continue with JSON output.");
                    //Console.ReadLine();

                    // Convert the Stream to a strongly typed RateCardPayload object.  
                    // You can also walk through this object to manipulate the individuals member objects. 
                    Console.WriteLine("Stored {0} records in {1}", payload.value.Count, new FileInfo(filename).FullName);
                }

                Console.WriteLine("JSON output complete.  Press ENTER to close.");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("{0} \n\n{1}", e.Message, e.InnerException != null ? e.InnerException.Message : ""));
                Console.ReadLine();
            }
        }

        public static string GetOAuthTokenFromAAD()
        {
            var authenticationContext = new AuthenticationContext(String.Format("{0}/{1}",
                                                                    ConfigurationManager.AppSettings["ADALServiceURL"],
                                                                    ConfigurationManager.AppSettings["TenantDomain"]));

            //Ask the logged in user to authenticate, so that this client app can get a token on his behalf
            var result = authenticationContext.AcquireToken(String.Format("{0}/", ConfigurationManager.AppSettings["ARMBillingServiceURL"]),
                                                            ConfigurationManager.AppSettings["ClientID"],
                                                            new Uri(ConfigurationManager.AppSettings["ADALRedirectURL"]));

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }

        public static string GetReportingUri(string billingServiceURL, string subscriptionId,
            DateTime from, DateTime to,
            AggregationGranularity granularity = AggregationGranularity.Daily,
            bool showDetails = true)
        {
            Func<DateTime, DateTime> cleanStartTime = (_) =>
            {
                _ = _.ToUniversalTime();
                return new DateTime(
                    year: _.Year,
                    month: _.Month,
                    day: _.Day,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    kind: DateTimeKind.Utc)
                .ToUniversalTime();
            };

            Func<DateTime, DateTime> cleanEndTime = (_) =>
            {
                _ = _.AddDays(1).AddMilliseconds(-1).ToUniversalTime();
                return new DateTime(
                    year: _.Year,
                    month: _.Month,
                    day: _.Day,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    kind: DateTimeKind.Utc)
                .ToUniversalTime();
            };

            const string dateTimeFormat = "yyyy-MM-dd HH:mm:ssZ";

            string requestURL = new Uri(string.Format("{0}/subscriptions/{1}/providers/Microsoft.Commerce/UsageAggregates",
                billingServiceURL, subscriptionId))
                     .AddQuery("api-version", "2015-06-01-preview")
                     .AddQuery("reportedstartTime", cleanStartTime(from).ToString(dateTimeFormat))
                     .AddQuery("reportedEndTime", cleanEndTime(to).ToString(dateTimeFormat))
                     .AddQuery("aggregationGranularity", granularity == AggregationGranularity.Daily ? "Daily" : "Hourly")
                     .AddQuery("showDetails", showDetails ? "true" : "false")
                     .AbsoluteUri;

            return requestURL;
        }

        public enum AggregationGranularity
        {
            Daily = 0,
            Hourly = 1
        }
    }

    public static class UriExtensions
    {
        public static Uri AddQuery(this Uri uri, string name, string value)
        {
            var ub = new UriBuilder(uri);
            var httpValueCollection = HttpUtility.ParseQueryString(uri.Query);
            httpValueCollection.Add(name, value);

            if (httpValueCollection.Count == 0)
            {
                ub.Query = String.Empty;
            }
            else
            {
                var sb = new StringBuilder();

                for (int i = 0; i < httpValueCollection.Count; i++)
                {
                    string text = httpValueCollection.GetKey(i);
                    {
                        text = HttpUtility.UrlEncode(text);

                        string val = (text != null) ? (text + "=") : string.Empty;
                        string[] vals = httpValueCollection.GetValues(i);

                        if (sb.Length > 0)
                            sb.Append('&');

                        if (vals == null || vals.Length == 0)
                            sb.Append(val);
                        else
                        {
                            if (vals.Length == 1)
                            {
                                sb.Append(val);
                                sb.Append(HttpUtility.UrlEncode(vals[0]));
                            }
                            else
                            {
                                for (int j = 0; j < vals.Length; j++)
                                {
                                    if (j > 0)
                                    {
                                        sb.Append('&');
                                    }
                                    sb.Append(val);
                                    sb.Append(HttpUtility.UrlEncode(vals[j]));
                                }
                            }
                        }
                    }
                }

                ub.Query = sb.ToString();
            }

            return ub.Uri;
        }
    }
}
