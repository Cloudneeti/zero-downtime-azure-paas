namespace ZeroDowntime.Core
{
    using Microsoft.Azure.Documents;

    public class NBMEUser : Resource
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public int Phone { get; set; }

        public string DataVersion { get; set; }
    }
}