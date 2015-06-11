# ConsoleApp-Billing-RateCard
---------------------------
This is a simple app that calls the Azure REST API for the RateCard to retrieve the list of resources per Azure offer, along with the pricing details for each resource. Once the logged-in user authenticates himself, the app gets the rateCard response and then deserializes the response and prints it to the console.  

Please note, the RateCard APIs are are currently in Preview.  Please refer to the MSDN article [Azure Billing REST API Reference (Preview)] (https://msdn.microsoft.com/library/azure/1ea5b323-54bb-423d-916f-190de96c6a3c) for details on the Billing RateCard API.

## How To Run This Sample

To run this sample you will need:

- Visual Studio 2013 or higher
- An Internet connection
- An Azure subscription (a free trial is sufficient)

You will also need to be comfortable with the following tasks:

- Using the Azure Management Portal (or working with your AAD administrator) to do configuration work 
- Using Git and Github to bring the sample down to your location machine
- Using Visual Studio to edit configuration files, build, and run the sample

Every Azure subscription has an associated AAD tenant.  If you don't already have an Azure subscription, you can get a free subscription by signing up at [http://wwww.windowsazure.com](http://www.windowsazure.com).  

### Step 1: Configure a Native Client application in your AAD tenant
Before you can run the sample application you will need to allow it to access your AAD tenant.  If you already have a Native Client Application configured that you would like to use, you can jump to Step 2.

To configure a new AAD application:

1. Log in to the [Azure management portal](http://manage.windowsazure.com), using credentials that have been granted service co-administrator access on the subscription which is trusting your AAD tenant, as well as Global Administrator access in the AAD tenant.
2. Select the AAD tenant you wish to use, and go to the "Applications" page.
3. From there, you can use the "Add" feature to "Add a new application my organization is developing".
4. Provide a name (ie: ConsoleApp-Billing-RateCard or similar) for the new application.
5. Be sure to select the "Native Client Application" type, specify a  valid URL for "Redirect URI", which can be http://localhost/ for the purposes of this sample, then click check mark to save.
6. After you've added the new application, select it again in the list of applications, so you can make sure the sample app has permissions to access the Windows Azure Service Management APIs.  
7. Then select "Configure", and scroll down to the to the "Permissions to other applications" section of your newly created application's configuration page.  Click the green "Add Application" button, select the "Windows Azure Service Management" row, and click the check mark to save.  Then  on the "Windows Azure Service Management" row, click the Delegated Permissions drop down list,  select the "Access Azure Service Management (preview)" option, and click "Save".  
8. While you are on this page, also note the "Client ID" GUID as you will use this and the key in step #3 below.
9. NOTE: the permission "Access your organization's directory" allows the application to access your organization's directory on behalf of the signed-in user - this is a delegation permission and must be consented by the Administrator for web apps (such as this demo app).
The permission "Enable sign-on and read users' profiles" allows users to sign in to the application with their organizational accounts and lets the application read the profiles of signed-in users, such as their email address and contact information - this is a delegation permission, and can be consented to by the user.
The other permissions, "Read Directory data" and "Read and write Directory data", are Delegation and Application Permissions, which only the Administrator can grant consent to.


Please refer to the prerequisites section in the "Azure AD Reports and Events" article in the MSDN library (under Services, Azure Active Directory, Graph API) for more depth on configuring an Azure AD tenant to enable an application to access your tenant.  

### Step 2:  Clone or download this repository

From your shell (ie: Git Bash, etc.) or command line, run the following command :

    git clone https://github.com/AzureADSamples/WebApp-GraphAPI-Reporting.git

### Step 3:  Edit, build, and run the sample in Visual Studio 2013
After you've configured your tenant and downloaded the sample app, you will need to go into the local sub directory in which the Visual Studio solution is stored (typically in <your-git-root-directory>\WebApp-GrapAPI-Reporting), and open the WebApp-GraphAPI-Reporting.sln Visual Studio solution.  Upon opening, navigate to the Web.config file and update the following key/value pairs, using your tenant and application configuration information from earlier :

    <add key="ida:Domain" value="MyTenant.onMicrosoft.com"/>                  		<!-- DNS name for your AAD tenant -->
    <add key="ida:ClientId" value="aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"/>    		<!-- GUID for your AAD application -->
    <add key="ida:AppKey" value="abcd1234abcd1234abcd1234abcd1234abcd1234abcd"/>	<!-- Secret key for your AAD application -->

When finished, you should be able to successfully build and run the application, which will present a web form UI which you can use for testing against your AAD tenant.

### Step 4:  Run the application with your own AAD tenant
Use the drop down list box at the top of the web page to select which endpoint you would like to call.  The first item in the list will invoke the $metadata endpoint, which will return the Service Metadata Document (CSDL).  The remaining items in the list will invoke all of the report endpoints, some with variants to demonstrate the supported query options.




