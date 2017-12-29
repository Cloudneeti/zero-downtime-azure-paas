using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace ZeroDowntime.Core
{
    public class Item
    {
        public DateTime RequestTime { get; set; }
        public string Version { get; set; }
        public string Summary { get; set; }
    }
}