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
    public class BuildShouldSucceedData
    {
        public static IEnumerable<object> TestData
        {
            get
            {
                var tags = Environment.GetEnvironmentVariable("SKIP_TAGS");
                string[] skipTags;
               
                if (tags != null)
                {
                    skipTags = tags.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToArray();  // a, b,,c
                    //skipTags = tags.Split(',').ToArray();
                }
                else
                {
                    skipTags = new string[0];
                }
                var include = Environment.GetEnvironmentVariable("INCLUDE_TESTS");
                string[] includeTests = null;
                if (include != null)
                {
                    includeTests = include.Split(',').ToArray();
                }
                var client = AppveyorApi.GetClient();
                var projectList = AppveyorApi.GetProjects(client).Result;
                var testCases = new List<object[]>();
                foreach (var p in projectList)
                {
                    var projectTags = p.tags.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToArray();
                    if (String.Equals(p.name, "build-tests", StringComparison.OrdinalIgnoreCase)
                        | skipTags.Intersect(projectTags, StringComparer.OrdinalIgnoreCase).Count() > 0 
                        | ((includeTests != null) && !(includeTests.Any(p.tags.Contains))))
                    
                    {
                        continue;
                    }
                    else
                    {
                        var x = new object[] { p.name };
                        testCases.Add(x);
                    }
                }
                return testCases;
            }
        }
    }

    public class Tests
    {
        private readonly ITestOutputHelper output;
        string account = "appveyor-tests";
        int MaxProvisioningTime = 9;
        int MaxRunTime = (Environment.GetEnvironmentVariable("MAX_BUILD_TIME_MINS") != null) ?
            int.Parse(Environment.GetEnvironmentVariable("MAX_BUILD_TIME_MINS")) : 7;        
        public Tests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [MemberData("TestData", MemberType = typeof(BuildShouldSucceedData))]
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
        }

        public async Task<bool> Run(string project)
        {
            HttpClient client = AppveyorApi.GetClient();
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
            var postBuild = await client.PostAsJsonAsync("builds", requestBody);
            var postBuildString = await postBuild.Content.ReadAsStringAsync();
            //Deserialize response to Build class
            Build build = JsonConvert.DeserializeObject<Build>(postBuildString);
            output.WriteLine("running version: {0} of project: {1}", build.version, project);
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
                //get last build and deserialize into ProjectModel class
                var lastBuild = await client.GetAsync("projects/" + account + "/" + project);
                var lastBuildString = await lastBuild.Content.ReadAsStringAsync();
                ProjectModel model = JsonConvert.DeserializeObject<ProjectModel>(lastBuildString);
                Job job = model.build.jobs.FirstOrDefault<Job>();
                output.WriteLine("Build status at " + elapsed.ToString() + " - " + job.status);

                if (String.Equals(job.status, "queued", StringComparison.OrdinalIgnoreCase) && elapsed.TotalMinutes > MaxProvisioningTime)
                {
                    await client.DeleteAsync("builds/" + account + "/" + project + "/" + build.version);
                    output.WriteLine(buildNotStartedErrorMessage);
                    return false;
                }
                else if (String.Equals(job.status, "running", StringComparison.OrdinalIgnoreCase) && elapsed.TotalMinutes > MaxRunTime)
                {
                    await client.DeleteAsync("builds/" + account + "/" + project + "/" + build.version);
                    output.WriteLine(buildNotFinishedErrorMessage);
                    return false;
                }
                else if (String.Equals(job.status, "failed", StringComparison.OrdinalIgnoreCase))
                {
                    output.WriteLine(buildFailedErrorMessage);
                    return false;
                }
                else if (String.Equals(job.status, "cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    output.WriteLine(buildCancelledErrorMessage);
                    return false;
                }
                else if (String.Equals(job.status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    var started = job.started;
                    var finished = job.finished;
                    output.WriteLine("Build duration: {0}", (finished - started));
                    return true;
                }

            } //end while loop
        }

    }
}
        
