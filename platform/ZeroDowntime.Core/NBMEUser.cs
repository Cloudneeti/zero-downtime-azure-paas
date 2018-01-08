namespace ZeroDowntime.Core
{
    using Microsoft.Azure.Documents;

    public class NBMEUser : Resource
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string ApplicationVersion { get; set; }
    }
}