using AzureRestHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleRestListResource
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
        /// Print JSON object idented on the console
        /// </summary>
        /// <param name="jContent">json data</param>
        static void PrintJsonData(string jContent)
        {
            var rootToken = JToken.Parse(jContent);
            string xx = JsonConvert.SerializeObject(rootToken, Formatting.Indented);
            Console.WriteLine(xx);
        }
        static void Main(string[] args)
        {
            
            string[] myCommands = new string[]
                {
                    //List VM 
                    "/subscriptions/{0}/providers/Microsoft.Compute/virtualMachines?api-version=2016-04-30-preview",
                    //List Public IPs
                    "subscriptions/{0}/providers/Microsoft.Network/publicIPAddresses?api-version=2016-09-01",
                    //List Public IP on RG ARMD-636238979770432643
                    "subscriptions/{0}/resourceGroups/ARMD-636238979770432643/providers/Microsoft.Network/publicIPAddresses?api-version=2016-09-01"
                };

            setup();

            _myToken = myTokenManager.GetToken(_tenant_id, _client_id, _client_secret, _managementUrl, _loginUrl).Result;

            foreach (string myCommand in myCommands)
            {
                string currentCommand = string.Format(myCommand, _subscription_id);
                string json = myTokenManager.ExecuteGet(currentCommand, _managementUrl, _myToken).Result;
                PrintJsonData(json);
                Console.ReadLine();
                Console.Clear();
            }
        }
    }
}
