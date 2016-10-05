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
    public class ProjectListFixture
    {
           
        string token = Environment.GetEnvironmentVariable("api_key");
        string baseUri = "https://ci.appveyor.com/api/";
        
        public ProjectListFixture()
        {
           
        }
        public async Task<List<string>> GetProjects(HttpClient client) 
        {
            var projectList = new List<string>();
            var response = await client.GetAsync("projects");
            var projectString = await response.Content.ReadAsStringAsync();
            var projectsParsed = JArray.Parse(projectString);
            var projects = projectsParsed.Children().ToList();
            foreach (var p in projects)
            {
                projectList.Add(p.Value<string>("name"));
            }
            return projectList;

        }
        public HttpClient GetClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(baseUri);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

    }
       
}
