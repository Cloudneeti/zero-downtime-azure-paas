using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace ZeroDowntime.Core
{
    public class NBMEUser
    {
        public string UserId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }
    }
}