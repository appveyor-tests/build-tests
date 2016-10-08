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
        public static IEnumerable<object> TestData
        {
            get
            {
                var tags = Environment.GetEnvironmentVariable("SKIP_TAGS");
                string[] skipTags;
                if (tags != null)
                {
                    skipTags = tags.Split(',').ToArray();
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
                    if (String.Equals(p.Key, "build-tests", StringComparison.OrdinalIgnoreCase) | skipTags.Any(p.Value.Contains))
                    {
                        continue;
                    }
                    else
                    {
                        var x = new object[] { p.Key };
                        testCases.Add(x);
                    }
                }
                return testCases;
            }
        }
    }
}
