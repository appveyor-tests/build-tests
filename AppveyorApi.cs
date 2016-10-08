using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace build_tests
{
    class AppveyorApi
    {
        public static async Task<Dictionary<string, string>> GetProjects(HttpClient client)
        {
            //fetch all projects from appeyor account and put them and their tags into a dictionary<string, string>
            var projectDict = new Dictionary<string, string>();
            var projectList = new List<string>();
            var response = await client.GetAsync("projects");
            var projectString = await response.Content.ReadAsStringAsync();
            var projectsParsed = JArray.Parse(projectString);
            var projects = projectsParsed.Children().ToList();
            foreach (var p in projects)
            {
                //var projectTagsString = p.Value<string>("tags");
                //var projectTags = JArray.Parse(projectTagsString);
                //projectList.Add(p.Value<string>("name"));
                projectDict.Add(p.Value<string>("name"), p.Value<string>("tags"));
            }
            return projectDict;
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
