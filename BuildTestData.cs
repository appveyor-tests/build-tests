using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace BuildTests
{
    public class BuildTestData
    {
        private readonly ITestOutputHelper output;
        public BuildTestData(ITestOutputHelper output)
        {
            this.output = output;
        }
        public static IEnumerable<object> TestData
        {
            get
            {
                var tags = Environment.GetEnvironmentVariable("SKIP_TAGS");
                string[] skipTags;
                if (tags != null)
                {
                    skipTags = tags.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToArray();  // a, b,,c
                }
                else
                {
                    skipTags = new string[0];
                }
                var includeTests = Environment.GetEnvironmentVariable("INCLUDE_TESTS");
                //var testBase = new TestBase();
                var testClient = TestBase.GetClient();
                var projectList = TestBase.GetProjects(testClient).Result;
                var testCases = new List<object[]>();
                foreach (var p in projectList)
                {
                    var projectTags = p.Tags != null
                        ? p.Tags.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToArray()
                        : new string[0];

                    if (String.Equals(p.Slug, "build-tests", StringComparison.OrdinalIgnoreCase)
                        | skipTags.Intersect(projectTags, StringComparer.OrdinalIgnoreCase).Count() > 0) // a,B,c,D   A,c,E => a,c
                    {
                        continue;
                    }
                    else
                    {
                        var x = new object[] { p };
                        testCases.Add(x);
                    }
                }
                return testCases;
            }
        }
    }
}
