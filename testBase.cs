using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Net.Http.Formatting;
using Xunit.Abstractions;
using Xunit.Extensions;

namespace BuildTests
{

    public class TestBase
    {
        private readonly ITestOutputHelper output;
        static string account = "appveyor-tests";
        static string token = Environment.GetEnvironmentVariable("appveyor_build_tests_api_key");
        static string baseUri = "https://ci.appveyor.com/api/";
        int MaxProvisioningTime = 9;
        int MaxRunTime = (Environment.GetEnvironmentVariable("MAX_BUILD_TIME_MINS") != null) ?
            int.Parse(Environment.GetEnvironmentVariable("MAX_BUILD_TIME_MINS")) : 7;
        

        public TestBase(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [MemberData("TestData", MemberType = typeof(BuildTestData))]
        public void BuildShouldSucceed(string project)
        {
            var result = Run(project).Result;
            if (result)
            {
                output.WriteLine("running project: {0} succeeded", project);
                Assert.True(result);
            }
            else
            {
                output.WriteLine("running project: {0} failed", project);
                Assert.True(result);
            }
            Task.Delay(TimeSpan.FromSeconds(30)).Wait();
        }

        public async Task<bool> Run(string project)
        {
            Console.WriteLine("Testing: " + project);
            HttpClient client = GetClient();
            //start the build
            var requestBody = new
            {
                accountName = account,
                projectSlug = project,
                branch = "master",
                environmentVariables = new
                {
                    APPVEYOR_BUILD_WORKER_CLOUD = Environment.GetEnvironmentVariable("APPVEYOR_BUILD_WORKER_CLOUD"),
                    APPVEYOR_BUILD_WORKER_IMAGE = Environment.GetEnvironmentVariable("APPVEYOR_BUILD_WORKER_IMAGE")
                    
                }
            };
            var response = await client.PostAsJsonAsync("builds", requestBody);
            var buildJson = await response.Content.ReadAsStringAsync();
            JToken build = JToken.Parse(buildJson);
            string buildVersion = build.Value<string>("version");
            output.WriteLine("running version: {0} of project: {1}", buildVersion, project);
            DateTime buildStarted = DateTime.UtcNow;
            string buildNotStartedErrorMessage = String.Format("Build has not started in {0} minutes", MaxProvisioningTime);
            string buildNotFinishedErrorMessage = String.Format("Build has not finished in {0} minutes", MaxRunTime);
            string buildFailedErrorMessage = String.Format("Build has failed");
            string buildCancelledErrorMessage = String.Format("Build has been cancelled");
            //checking to see when build status is success. 
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));               
                var elapsed = DateTime.UtcNow - buildStarted;
                //get and parse last build which contains jobs array
                var lastBuild = await client.GetAsync("projects/" + account + "/" + project);
                var lastBuildJson = await lastBuild.Content.ReadAsStringAsync();
                JObject buildObject = JObject.Parse(lastBuildJson);
                var job = (JToken)buildObject["build"]["jobs"][0];
                var jobId = job.Value<string>("jobId");
                var status = job.Value<string>("status");

                output.WriteLine("Build status at " + elapsed.ToString() + " - " + status);

                if (String.Equals(status, "queued", StringComparison.OrdinalIgnoreCase) && elapsed.TotalMinutes > MaxProvisioningTime)
                {
                    await client.DeleteAsync("builds/" + account + "/" + project + "/" + buildVersion);
                    output.WriteLine(buildNotStartedErrorMessage);
                    return false;
                }
                else if (String.Equals(status, "running", StringComparison.OrdinalIgnoreCase) && elapsed.TotalMinutes > MaxRunTime)
                {
                    await client.DeleteAsync("builds/" + account + "/" + project + "/" + buildVersion);
                    output.WriteLine(buildNotFinishedErrorMessage);
                    return false;
                }
                else if (String.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
                {
                    output.WriteLine(buildFailedErrorMessage);
                    return false;
                }
                else if (String.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    output.WriteLine(buildCancelledErrorMessage);
                    return false;
                }
                else if (String.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    var started = job.Value<DateTime>("started");
                    var finished = job.Value<DateTime>("finished");
                    output.WriteLine("Build duration: {0}", (finished - started));
                    return true;
                }

            } //end while loop
        }

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
        
