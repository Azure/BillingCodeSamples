# WebApp-Billing-MultiTenant
---------------------------
This code sample creates a Web App that allows the logged-in user to give consent to the application, to call the Azure Graph API and the Azure Usage API on the user's behalf. It also shows the OAuth flows required to get consent for the ‘Reader’ role, for the list of Microsoft Azure subscriptions that the user wants to allow access to.  

The application first lists all subscriptions that the signed-in user has access to, and then lets the user choose which subscription to use for pulling Usage data. After the subscription is selected, the app retrieves and deserializes the Usage response data, and displays it on the page. 

This sample also demonstrates the MultiTenant functionality in Azure Active Directory (AAD), which is required in partner/ISV scenarios where you are building a Web App that needs consent from owners of multiple AAD tenants, to grant access to their directories. 

Please note, the Usage APIs were in Preview at the time of this writing. Please refer to MSDN article 
[Azure Billing REST API Reference (Preview)](https://msdn.microsoft.com/library/azure/1ea5b323-54bb-423d-916f-190de96c6a3c) for additional details on the Billing REST APIs. 

## How To Run This Sample

To run this sample you will need:

- Visual Studio 2013 or higher (you can get a free version [here](https://www.visualstudio.com/en-us/products/free-developer-offers-vs.aspx))
- An Internet connection
- An Azure subscription (a free trial is sufficient)

You will also need to be comfortable with the following tasks:

- Using the [Azure Management Portal](https://manage.windowsazure.com) (or working with your administrator) to do configuration work 
- Using Git and Github to bring the [sample code](https://github.com/Azure/BillingCodeSamples) down to your location machine
- Using Visual Studio to edit configuration files, build, and run the sample

Every Azure subscription has an associated AAD tenant.  If you don't already have an Azure subscription, you can get a free subscription by signing up at [http://wwww.windowsazure.com](http://www.windowsazure.com).  

### Step 1: Configure a Web App application in your AAD tenant
Before you can run the sample application, you will need to allow it to access your AAD tenant for authentication and authorization to access the Graph and Billing APIs.  

To configure a new AAD application:

1. Log in to the [Azure Management Portal](http://manage.windowsazure.com), using credentials that have been granted service administrator or co-administrator access on the subscription which is trusting your AAD tenant, and granted Global Administrator access in the AAD tenant.  See [Manage Accounts, Subscriptions, and Administrative Roles](https://msdn.microsoft.com/library/azure/hh531793.aspx) for details on managing the service administrator and co-administrators.

2. Select the AAD tenant you wish to use, and go to the "Applications" page.

3. From there, select the "Add" feature to "Add a new application my organization is developing".

4. Provide a name (ie: WebApp-Billing-MultiTenant or similar) for the new application.

5. Be sure to select the "Web Application and/or Web API" type, then click the "next" arrow.

6. Then specify a valid "Sign-on URL" and "App ID URI". Both must be valid well-formed, and can be changed later.
	- The "Sign-on URL" (App URL) specifies where users can sign-in and use your application.  For the purposes of this sample, you can specify `http://localhost/`.  
	- The "App ID URI" provides a unique/logical identifier which AAD associates with this application. In order to configure this application as Multi-Tenant, the "App ID URI" must be in a verified custom domain for an external user to grant your application access to their AAD data (ie: `xxxx.Test.OnMicrosoft.Com`, if your directory domain is `Test.OnMicrosoft.Com`).  It must also be unique within your directory, and therefore not being used as the "App ID URI" for any other applications.
	- Click the check mark to save.

7. After you've added the new application, select it again within the list of applications and click "Configure" so you can make the following additional changes:
	- Under "Application is multi-tenant", select "Yes".  Again, this setting indicates that your app can authenticate, and will require consent from, users from multiple AAD tenants.
	- Under "Keys", click the "Select Duration" dropdown list and pick an appropriate value.
	
		**NOTE**: This key is used as a password for authentication in conjunction with the Client ID GUID, and is not immediately visible.  As the comment indicates, it will become visible after you save all configuration changes later (below), at which point YOU MUST SAVE A COPY, as it will no longer be available for viewing in the Management Portal UI.

	- Under "Reply URL", enter `http://<domain>/Account/SignIn`, where `domain` is the DNS name where the Web App will be hosted.  For testing purposes, you will run this application in a locally hosted instance of IIS, so you will need to use `localhost:NNNNN`, where `NNNNN` = the port being used by IIS. When you're finished, the URL should look similar to `http://localhost:62080/Account/SignIn`
	- Under "Permissions to other applications", click the "Add Application" button, select the "Windows Azure Service Management API" row, and click the check mark to save.  After saving, hover the "Delegated Permissions" area on the right side of the "Windows Azure Service Management" row, click the "Delegated Permissions" drop down list, and select the "Access Azure Service Management (preview)" option   This will ensure the sample application will have permissions to access the Windows Azure Service Management APIs, which is the permission used to secure the Billing APIs.  

    	**NOTE**: the "Windows Azure Active Directory" permission "Enable sign-on and read users' profiles" is enabled by default.  This allows users to sign in to the application with their organizational accounts, enabling the application to read the profiles of signed-in users, such as their email address and contact information.  This is a delegated permission, which gives the user the ability to consent before proceeding.  Please refer to [Adding, Updating, and Removing an Application](https://msdn.microsoft.com/en-us/library/azure/dn132599.aspx) for more depth on configuring an Azure AD tenant to enable an application to access your tenant.

8. Finally, click "Save" at the bottom of the page to save all of the above configuration changes.

9. While you are on this page, also note/copy the "Client ID" GUID and the client "Key", as you will use these in Step #3 below.  As mentioned above, YOU MUST SAVE A COPY of this key, as it will no longer be available for viewing in the Management Portal UI.

### Step 2:  Clone or download the BillingCodeSamples repository

From your shell (ie: Git Bash, etc.) or command line, run the following command :

    git clone https://github.com/Azure/BillingCodeSamples.git

### Step 3:  Edit and Build the sample in Visual Studio
After you've configured your tenant and downloaded the sample app, you will need to go into the local sub directory in which the Visual Studio solution is stored (typically in `<your-git-root-directory>\BillingCodeSamples`), and open the `ConsoleApp-Billing-MultiTenant.sln` Visual Studio solution.  Upon opening, navigate to the `web.config` file and update the following key/value pairs, using your "Client ID" GUID and the client "Key" configuration information from earlier.  NOTE: It's very important that all values match your configuration!

    <add key="ida:ClientID" value="ENTER-CLIENT-ID-GUID" />
    <add key="ida:Password" value="ENTER-CLIENT-KEY" />

The static method `GetUsage()`, which is defined in `AzureResourceManagerUtil.cs`, is responsible for calling the Usage API for the specified subscription, after getting the token for the logged-in user.  If you would like to query for a different time range, you can change the `reportedStartTime` and `reportedEndTime` parameter values in this method accordingly :  

	string requesturl = String.Format("https://management.azure.com/subscriptions/{0}/providers/Microsoft.Commerce/UsageAggregates?api-version=2015-06-01-preview&reportedstartTime=2015-05-15+00%3a00%3a00Z&reportedEndTime=2015-05-16+00%3a00%3a00Z", subscriptionId);

When you build the solution, it will also restore the missing Nuget packages which contain libraries upon which the sample depends.  

### Step 4:  Run the application against your AAD tenant and the Billing APIs

When finished with Step 3, you should be able to successfully run the application locally, and will be greeted with the following Sign-In page in your browser where you can select the type of credentials you would like to use for authentication :

<center>![Sign-In] [1]</center>

**Note**: The credentials you use for testing the application have 2 requirements :

- The Azure Billing REST APIs are implemented as a Resource Provider as part of the Azure Resource Manager, and therefore share its dependencies.  Access control for Azure Resource Manager uses the built-in Owner, Contributor, and Reader roles, via the Role Based Access Control (RBAC) feature in the [Azure Preview Portal](https://portal.azure.com/).  Therefore, you must make sure that the logged-in user is a member of either the ‘Reader’, ‘Owner’ or ‘Contributor’ roles for the specified subscription.  By default, all Azure service administrator accounts are members of the Owner role. For details, see [Role-based access control in the Microsoft Azure portal](https://azure.microsoft.com/en-us/documentation/articles/role-based-access-control-configure/). 

- In addition, in order to access the Usage data for the Azure subscription, the credentials you use for sign-in must be granted service administrator or co-administrator access on the subscription.

If you elect to sign-in with a Microsoft Account, you will first be prompted to specify the DNS name for the AAD tenant (ie: `contoso.com`, `BLTest.OnMicrosoft.Com`, etc.)  

<center>![Sign-In] [2]</center>

After you select the type of credentials you will use for sign-in, you will be redirected to the appropriate page for entering your credentials.  The first example below is the page used for work or school account (aka: Org ID), the second is for Microsoft account :

<center>![Sign-In] [3]</center>
<center>![Sign-In] [4]</center>

After you successfully sign-in, the first page you see will be similar to the one below, which shows you the subscriptions available to you, given the credentials you used for sign-in.  On this page, you will want to pick the subscription you are interested in seeing Usage details for, by clicking the "Connect" button :

<center>![Sign-In] [5]</center>

Finally, the last page you will see will show you the Usage consumption details for the subscription you selected, similar to the one below:

<center>![Sign-In] [6]</center>

<!--Image references-->
[1]: Readme-Sign-In.PNG
[2]: Readme-Sign-In-LiveID-TenantDNS.PNG
[3]: Readme-Sign-In-OrgID.PNG
[4]: Readme-Sign-In-LiveID.PNG
[5]: Readme-Subscription.PNG
[6]: Readme-Usage.PNG













