namespace ZeroDowntime.Functions
{
    using ZeroDowntime.Core;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Azure.WebJobs.Host;
    using System.Collections.Generic;

    /// <summary>
    /// azure function that access data from cosmos db
    /// and is triggered by http call from web app
    /// </summary>
    public static class CosmosDataAccessFunc
    {
        [FunctionName(nameof(CosmosDataAccessFunc))]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string endpoint = "https://zdcosmosdb.documents.azure.com:443/";
            string authkey = "8eIG2CLmqhjBLDWAOv9II2zLoY65WxOwKnmShyYq1I3TZStzRHIFejWIgczFvC2zi2SmTcEtr1mtpTNFdBdyXw==";
            //string  endpoint = ConfigurationManager.AppSettings["endpoint"];
            //string  authKey = ConfigurationManager.AppSettings["authkey"];
            DocumentDBRepository<Item>.Initialize(endpoint, authkey);
            if (req.Method == HttpMethod.Get)
            {
                var results = await DocumentDBRepository<Item>.GetItemsAsync("ToDoList", "Items");
                return req.CreateResponse(HttpStatusCode.OK, results);

            }
            else
            {
                dynamic data = await req.Content.ReadAsAsync<object>();
                string name = data?.name;
                string description = data?.description;
                Item item = new Item();
                item.Name = name;
                item.Description = description;
                await DocumentDBRepository<Item>.CreateItemAsync(item, "ToDoList", "Items");
                return req.CreateResponse(HttpStatusCode.Created);
            }
        }
    }
}
