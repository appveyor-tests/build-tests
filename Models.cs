using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTests
{
    public class ProjectModel
    {
        public Project project { get; set; }
        public Build build { get; set; }
    }

    public class Project
    {
        public int projectId { get; set; }
        public string name { get; set; }
        public string tags { get; set; }
        public List<Build> builds { get; set; }
    }

    public class Job
    {
        public string jobId { get; set; }
        public string status { get; set; }
        public DateTime started { get; set; }
        public DateTime finished { get; set; }

    }

    public class Build
    {
        public int buildId { get; set; }
        public List<Job> jobs { get; set; }
        public string version { get; set;  }
        public string branch { get; set; } 
        public string status { get; set; }
        public string started { get; set; }
        public string finished { get; set; }
        public string created { get; set; }
        public string updated { get; set; }
    }
}
