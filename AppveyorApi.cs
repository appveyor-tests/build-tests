using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BuildTests
{
    public static class AppveyorApi
    {
        static string token = Environment.GetEnvironmentVariable("appveyor_build_tests_api_key");
        static string baseUri = "https://ci.appveyor.com/api/";
        public static async Task<List<Project>> GetProjects(HttpClient client)
        {
            //fetch all projects from appeyor account and put them and their tags into a dictionary<string, string>     
            var response = await client.GetAsync("projects");
            string responseString = await response.Content.ReadAsStringAsync();
            List<Project> projects = JsonConvert.DeserializeObject<List<Project>>(responseString);
            return projects;           
        }

        public static HttpClient GetClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(baseUri);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}
