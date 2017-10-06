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
                string[] skipTags;
                string[] includeTags;

                var evSkipTags = Environment.GetEnvironmentVariable("SKIP_TAGS");
                if (evSkipTags != null)
                {
                    skipTags = evSkipTags.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToArray();  // a, b,,c
                }
                else
                {
                    skipTags = new string[0];
                }

                var evIncludeTags = Environment.GetEnvironmentVariable("INCLUDE_TAGS");
                if (evIncludeTags != null)
                {
                    includeTags = evIncludeTags.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToArray();  // a, b,,c
                }
                else
                {
                    includeTags = new string[0];
                }

                //var testBase = new TestBase();
                var testClient = TestBase.GetClient();
                var projectList = TestBase.GetProjects(testClient).Result;
                var testCases = new List<object[]>();
                foreach (var p in projectList)
                {
                    var projectTags = p.Tags != null
                        ? p.Tags.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToArray()
                        : new string[0];

                    if (String.Equals(p.Slug, "build-tests", StringComparison.OrdinalIgnoreCase))
                    {
                        // skip "build-tests" project
                        continue;
                    }

                    if (skipTags.Intersect(projectTags, StringComparer.OrdinalIgnoreCase).Count() > 0) // a,B,c,D   A,c,E => a,c
                    {
                        // skip tags
                        continue;
                    }

                    if (includeTags.Length > 0 && includeTags.Intersect(projectTags, StringComparer.OrdinalIgnoreCase).Count() == 0)
                    {
                        // include tags
                        continue;
                    }

                    var x = new object[] { p };
                    testCases.Add(x);
                }

                Console.WriteLine("Projects found: " + testCases.Count);

                return testCases;
            }
        }
    }
}
