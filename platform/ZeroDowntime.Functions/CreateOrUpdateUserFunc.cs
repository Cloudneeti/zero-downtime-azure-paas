
namespace ZeroDowntime.Functions
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Azure.WebJobs.Host;
    using Newtonsoft.Json;

    using ZeroDowntime.Core;

    public static class CreateOrUpdateUserFunc
    {
        [FunctionName(nameof(CreateOrUpdateUserFunc))]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            try
            {
                // Get request body
                var data = await req.Content.ReadAsStringAsync();

                var user = JsonConvert.DeserializeObject<NBMEUser>(data);

                await DocumentDBRepository.UpsertNbmeUser(user);

                return req.CreateResponse(HttpStatusCode.Created);
            }
            catch(Exception ex)
            {
                log.Error(ex.Message);
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
