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

namespace build_tests
{
    public class TestBase : IClassFixture<ProjectListFixture>
    {
        private readonly ITestOutputHelper output;
        string account = "appveyor-tests";
        List<string> projectList;
        HttpClient client;
        int MaxProvisioningTime = 9;
        int MaxRunTime = 5;
        public TestBase(ProjectListFixture fixture, ITestOutputHelper output)
        {
            //fetch all projects from appeyor account and put them into a list<string>
            this.client = fixture.GetClient();
            this.projectList = fixture.GetProjects(this.client).Result;
            this.output = output;
           
        }
        [Fact]
        public void TestLoop()
        {
            foreach (string p in projectList)
            {
                if(String.Equals(p, "build-tests", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var result = Run(p).Result;
                if (result)
                {
                    output.WriteLine("running project: {0} succeeded", p);
                    Assert.True(result);
                }
                else
                {
                    output.WriteLine("running project: {0} failed", p);
                    Assert.True(result);
                }
            }
        }
        public async Task<bool> Run(string project)
        {
            //start the build
            var requestBody = new Dictionary<string, string>
            {
                { "accountName", account },
                { "projectSlug", project },
                { "branch", "master" }
            };
            var response = await client.PostAsync("builds", new FormUrlEncodedContent(requestBody));
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
                //output.WriteLine(lastBuildJson);

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
    }
}
        
