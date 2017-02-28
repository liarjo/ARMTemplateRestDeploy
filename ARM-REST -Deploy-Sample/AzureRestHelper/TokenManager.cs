using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AzureRestHelper
{
    public class ARMRestHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tenant_id"></param>
        /// <param name="client_id"></param>
        /// <param name="client_secret"></param>
        /// <param name="managementUrl"></param>
        /// <param name="loginUrl"></param>
        /// <returns></returns>
        public async Task<string> GetToken(string tenant_id, string client_id, string client_secret, string managementUrl, string loginUrl)
        {

            string myToken = null;

            var content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]{
                new KeyValuePair<string, string>("resource", managementUrl),
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", client_id),
                new KeyValuePair<string, string>("client_secret",client_secret) });
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.PostAsync(loginUrl + tenant_id + "/oauth2/token", content).Result;
                string stringR = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(stringR);
                JObject jsonR = JObject.Parse(stringR);
                myToken = jsonR.SelectToken("access_token").ToString();
            }

            return myToken;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="managementUrl"></param>
        /// <param name="myToken"></param>
        /// <returns></returns>
        public async Task<string> ExecuteGet(string command, string managementUrl, string myToken)
        {
            string stringR = null;
            using (var client = new HttpClient())
            {
                //Common headers
                client.BaseAddress = new Uri(managementUrl);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer  " + myToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await client.GetAsync(command);
                stringR = await response.Content.ReadAsStringAsync();
            }
            return stringR;
        }

         
        static async Task<string> ExecuteHttpPost(string command, HttpContent myContent, string managementUrl, string myToken)
        {
            string stringR = null;
            using (var client = new HttpClient())
            {
                //Common headers
                client.BaseAddress = new Uri(managementUrl);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer  " + myToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.PutAsync(command, myContent).Result;
                stringR = await response.Content.ReadAsStringAsync();
               
            }
            return stringR;
        }
    }
}
