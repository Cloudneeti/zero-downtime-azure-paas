namespace ZeroDowntime.Functions
{
    using System;
    using ZeroDowntime.Core;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Azure.WebJobs.Host;
    using System.Collections.Generic;
    using System.Configuration;
    using Newtonsoft.Json;
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

            try
            {
                //string endpoint = "https://zdcosmosdb.documents.azure.com:443/";
                //string authkey = "8eIG2CLmqhjBLDWAOv9II2zLoY65WxOwKnmShyYq1I3TZStzRHIFejWIgczFvC2zi2SmTcEtr1mtpTNFdBdyXw==";
                string endpoint = ConfigurationManager.AppSettings["CosmosDbEndpoint"];
                string authKey = ConfigurationManager.AppSettings["CosmosDbKey"];
                DocumentDBRepository<Item>.Initialize(endpoint, authKey);
                if (req.Method == HttpMethod.Get)
                {
                    var results = await DocumentDBRepository<Item>.GetItemsAsync("ToDoList", "Items");
                    return req.CreateResponse(HttpStatusCode.OK, new
                    {
                        items = results
                    });
                }
                else
                {
                    var input = await req.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<Item>(input);
                    
                    var createdItem=await DocumentDBRepository<Item>.CreateItemAsync(data, "ToDoList", "Items");
                    var item = await DocumentDBRepository<Item>.GetItemAsync(createdItem.SelfLink);
                    return req.CreateResponse(HttpStatusCode.Created, item);
                }
            }
            catch(Exception ex)
            {
                log.Error(ex.Message);
                return req.CreateResponse(HttpStatusCode.OK);
            }
        }
    }
}
