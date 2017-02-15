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

        }
        /// <summary>
        /// Create Token to access to Managment API
        /// </summary>
        /// <param name="tenant_id">Tenant ID</param>
        /// <param name="client_id">Client ID</param>
        /// <param name="client_secret">Client Secret</param>
        /// <param name="callback">Callback to set Token value</param>
        static async void GetToken(string tenant_id, string client_id, string client_secret,Action<string> callback)
        {

            string myToken=null;

            var content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]{
                new KeyValuePair<string, string>("resource", _managementUrl),
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", client_id),
                new KeyValuePair<string, string>("client_secret",client_secret) });
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.PostAsync(_loginUrl + tenant_id + "/oauth2/token", content).Result;
                string stringR = await response.Content.ReadAsStringAsync();
                Console.WriteLine(stringR);
                JObject jsonR = JObject.Parse(stringR);
                myToken = jsonR.SelectToken("access_token").ToString();
            }

            if (callback != null)
                callback(myToken);
        }
        /// <summary>
        /// Set Token Value
        /// </summary>
        /// <param name="myToken"></param>
        static void SetToken(string myToken)
        {
            _myToken = myToken;
            Console.WriteLine();
            Console.WriteLine("Token: {0}" + _myToken);
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
        /// Execute HTTP POST REST API CALL
        /// </summary>
        /// <param name="command">URL PATH</param>
        /// <param name="myContent">content</param>
        /// <param name="CallBack">callbacjk to write response</param>
        static async void ExecuteHttpPost(string command, HttpContent myContent, Action<string> CallBack)
        {
            using (var client = new HttpClient())
            {
                //Common headers
                client.BaseAddress = new Uri(_managementUrl);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer  " + _myToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.PutAsync(command, myContent).Result;
                string stringR = await response.Content.ReadAsStringAsync();
                if (CallBack != null)
                    CallBack(stringR);
            }
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

            ExecuteHttpPost(command, myContent, printDeployResponse);


        }
        /// <summary>
        /// Parse JSON data and print on the 
        /// </summary>
        /// <param name="jContent">json data</param>
        static void printDeployResponse(string jContent)
        {
            var rootToken = JToken.Parse(jContent);
            if (rootToken is JObject)
            {
                JObject oContent = JObject.Parse(jContent);
                Console.WriteLine("--------------------------------------------");
                foreach (var item in oContent)
                {
                    if (!oContent[item.Key].Any())
                        Console.WriteLine("{0} = {1}", item.Key, item.Value);
                    else
                    {
                        Console.WriteLine("{0}", item.Key);
                        printDeployResponse(oContent[item.Key].ToString());
                    }
                }
            }
            else
            {
                JArray aContent = JArray.Parse(jContent);
                foreach (JToken arrayElement in aContent)
                {
                    if (arrayElement.Any())
                        printDeployResponse(Newtonsoft.Json.JsonConvert.SerializeObject(arrayElement));
                    else
                        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(arrayElement));
                }
            }
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
            string Response="";
            ExecuteHttpPost(command, myContent, delegate (string s) { Response = s;  });
            Console.WriteLine();
            Console.WriteLine(Response);
            
        }
       
        static void Main(string[] args)
        {
            setup();

            GetToken(_tenant_id, _client_id, _client_secret, myToken => SetToken(myToken));
            
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
            DeployTemplate(_subscription_id, _resource_group_name, "depploy-" + DateTime.Now.Second.ToString(), jUpdatedBody);
            Console.ReadLine();
        }
    }
}
