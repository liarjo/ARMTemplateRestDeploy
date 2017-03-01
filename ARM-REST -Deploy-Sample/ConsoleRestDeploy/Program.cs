using AzureRestHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleRestDeploy
{
    class Program
    {
        static string _loginUrl;
        static string _subscription_id;
        static string _managementUrl;
        static string _client_id;
        static string _tenant_id;
        static string _client_secret;
        static string _myToken;
        static string _resource_group_name;
        static ARMRestHelper myTokenManager;
        /// <summary>
        /// Setup application configuration
        /// </summary>
        static void setup()
        {
            _loginUrl = ConfigurationManager.AppSettings["_loginUrl"];
            _managementUrl = ConfigurationManager.AppSettings["_managementUrl"];
            _client_id = ConfigurationManager.AppSettings["_client_id"];
            _tenant_id = ConfigurationManager.AppSettings["_tenant_id"];
            _client_secret = ConfigurationManager.AppSettings["_client_secret"];
            _resource_group_name = ConfigurationManager.AppSettings["_resource_group_name"];
            _subscription_id = ConfigurationManager.AppSettings["_subscription_id"];
            myTokenManager = new AzureRestHelper.ARMRestHelper();
        }

        /// <summary>
        /// Update parameter values on json deploy BODY
        /// </summary>
        /// <param name="parameters">Parameters and values to update</param>
        /// <param name="jdeployBody">json deploy BODY rest call</param>
        /// <returns></returns>
        static string UpdateParameters(List<KeyValuePair<string, string>> parameters, string jdeployBody)
        {
            
            JObject X = JObject.Parse(jdeployBody);

            foreach (JProperty item in X["properties"]["parameters"])
            {
                KeyValuePair<string, string> pa = parameters.Where(p => p.Key == item.Name).FirstOrDefault();
                if (pa.Value != null)
                {
                    item.Value["value"] = pa.Value;
                }
                Console.WriteLine(item.Name + " = " + item.Value["value"]);

                Console.WriteLine();
            }

            Console.WriteLine(X.ToString());

            return X.ToString();
        }
        /// <summary>
        /// Deploy Template
        /// </summary>
        /// <param name="subscriptionId">Sub ID</param>
        /// <param name="resourceGroupName">resource group name</param>
        /// <param name="deploymentName">deployment name</param>
        /// <param name="jsonBody">json REST call body</param>
        static void DeployTemplate(string subscriptionId, string resourceGroupName, string deploymentName, string jsonBody)
        {
            string manageURL = "/subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Resources/deployments/{2}?api-version=2016-09-01";
            var myContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            string command = string.Format(manageURL, subscriptionId, resourceGroupName, deploymentName);
            var jsonResponse=myTokenManager.ExecuteHttpPost(command, myContent, _managementUrl, _myToken).Result;
            printDeployResponse(jsonResponse);
        }
        /// <summary>
        /// Print on Console JSON data Indented
        /// </summary>
        /// <param name="jContent">json data</param>
        static void printDeployResponse(string jContent)
        {
            var rootToken = JToken.Parse(jContent);
            string xx = JsonConvert.SerializeObject(rootToken, Formatting.Indented);
            Console.WriteLine(xx);
        }
        /// <summary>
        /// Create or Update a resource group on specific region
        /// </summary>
        /// <param name="subscriptionId">Sibscription ID</param>
        /// <param name="resourceGroupName">RG Name</param>
        /// <param name="Location">Location Name</param>
        static void CreateOrUpdateRG(string subscriptionId, string resourceGroupName, string Location)
        {
            string myUniqTag = "sample TAG";
            string manageURL = "subscriptions/{0}/resourcegroups/{1}?api-version=2015-01-01";
            var myContent = new StringContent(
                                    "{\"location\":\"West US\",\"tags\":{\"provider\":\"" + myUniqTag + "\"}}",
                                    Encoding.UTF8,
                                    "application/json");
            string command = string.Format(manageURL, subscriptionId, resourceGroupName);
            string Response = myTokenManager.ExecuteHttpPost(command, myContent, _managementUrl, _myToken).Result;
            Console.WriteLine();
            Console.WriteLine(Response);
        }

        /// <summary>
        /// Wait until deploy change to status Succeded 
        /// </summary>
        /// <param name="RGName">Resource Group Name</param>
        /// <param name="DeployName">Deployment Name</param>
        static void waitDeploy(string RGName, string DeployName)
        {
            string command = String.Format(
                "subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Resources/deployments/{2}?api-version=2016-09-01",
                _subscription_id,
                RGName,
                DeployName
                );

            string provisioningState = "";
            while (provisioningState!= "Succeeded")
            {
                var rString = myTokenManager.ExecuteGet(command, _managementUrl, _myToken).Result;
                JObject myResponse = JObject.Parse(rString);
                provisioningState = myResponse.SelectToken("properties").SelectToken("provisioningState").ToString();

                Console.WriteLine("{1} provisioningState: {0}", provisioningState, RGName);

                System.Threading.Thread.Sleep(5 * 1000);
            }
            Console.WriteLine("");
        }
        static void Main(string[] args)
        {
            setup();
            var myTokenManager = new AzureRestHelper.ARMRestHelper();

            _myToken= myTokenManager.GetToken(_tenant_id, _client_id, _client_secret, _managementUrl, _loginUrl).Result;
            //Reasource Group Name
            if (string.IsNullOrEmpty(_resource_group_name))
                _resource_group_name = string.Format("ARMD-{0}", DateTime.Now.Ticks.ToString());
            Console.Write("Resourse Group: {0}", _resource_group_name);
            CreateOrUpdateRG(_subscription_id, _resource_group_name, "West US");

            //Set same parameters Values
            List<KeyValuePair<string, string>> parametersCollection = new List<KeyValuePair<string, string>>();
            parametersCollection.Add(new KeyValuePair<string, string>("dnsLabelPrefix", "jpgg" + DateTime.Now.Second.ToString()));
            parametersCollection.Add(new KeyValuePair<string, string>("adminPassword", "P@ssw0rd" + DateTime.Now.Second.ToString()));
            
            //json deployment BODY rest call
            string jBody = File.ReadAllText("deployTemplateBody.json");
            
            //Update json Body rest call
            string jUpdatedBody = UpdateParameters(parametersCollection, jBody);

            //Deploy template
            string deployName = "depploy-" + DateTime.Now.Second.ToString();
            DeployTemplate(_subscription_id, _resource_group_name, deployName, jUpdatedBody);

            //Wait for dpeloyment
            waitDeploy(_resource_group_name, deployName);

            Console.WriteLine("Enter to close");
            Console.ReadLine();
        }
    }
}
