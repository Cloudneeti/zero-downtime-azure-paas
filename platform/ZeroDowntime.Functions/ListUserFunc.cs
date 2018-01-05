namespace ZeroDowntime.Functions
{
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Azure.WebJobs.Host;
    using ZeroDowntime.Core;

    public static class ListUserFunc
    {
        [FunctionName(nameof(ListUserFunc))]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string endpoint = ConfigurationManager.AppSettings["CosmosDbEndpoint"];
                string authKey = ConfigurationManager.AppSettings["CosmosDbKey"];
                DocumentDBRepository.Initialize(endpoint, authKey);

            var results = await DocumentDBRepository.GetUsersAsync();
                    return req.CreateResponse(HttpStatusCode.OK,results);
        }
    }
}
