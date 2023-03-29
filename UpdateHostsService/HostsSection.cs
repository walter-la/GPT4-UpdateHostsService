using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateHostsService
{
    public class HostsSection
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public int IntervalInSeconds { get; set; }
    }
}
