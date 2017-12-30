using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZeroDowntime.Models;
using Newtonsoft.Json;
using System.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;

namespace ZeroDowntime.WebApp.Controllers
{
    public class ItemController : Controller
    {
        private static RequestTelemetry telemetryRequest = new RequestTelemetry();
        private static TelemetryClient telemetryClient = new TelemetryClient()
        {
            InstrumentationKey = ConfigurationManager.AppSettings["APPINSIGHTS_INSTRUMENTATIONKEY"]
        };

        public ItemController()
        {
            ItemController.CreateTelemetryClient();
        }

        private static void CreateTelemetryClient()
        {
            telemetryRequest.GenerateOperationId();
            telemetryClient.Context.Operation.Id = telemetryRequest.Id;
            telemetryRequest.Context.Operation.Name = $"GetItems-{ConfigurationManager.AppSettings["WebAppVersion"]}";
        }
        // GET: Item
        public ActionResult Index()
        {
            var requestStartTime = DateTime.UtcNow;
            string data = string.Empty;

            ViewBag.WebAppVersion = ConfigurationManager.AppSettings["WebAppVersion"];
            string requesturi = ConfigurationManager.AppSettings["MiddleTierEndpoint"];
            var requestTime = DateTime.UtcNow;
            Task.Run(
                async () =>
                {
                    data = await HttpHelper.PostAsync(requesturi,
                        JsonConvert.SerializeObject(
                            new Item
                            {
                                RequestTime = requestTime,
                                Version = ConfigurationManager.AppSettings["WebAppVersion"],
                                Summary=$"request to version {ConfigurationManager.AppSettings["WebAppVersion"]} made on {requestTime}"
                            }));
                    
                }).Wait();


            var items = JsonConvert.DeserializeObject<Item>(data);


            telemetryClient.TrackRequest($"Get-{ConfigurationManager.AppSettings["WebAppVersion"]}", requestStartTime, DateTime.UtcNow - requestStartTime, "200", true);
            //var items = new Item[] { new Item {Id="01",Description="tewst",Name="asdsds" } };
            return View(items);
        }

        [ActionName("Create")]
        public ActionResult CreateAsync()
        {
            ViewBag.WebAppVersion = ConfigurationManager.AppSettings["WebAppVersion"];
            return View();
        }

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAsync([Bind(Include = "Id,Name,Description,Summary")] Item item)
        {
            ViewBag.WebAppVersion = ConfigurationManager.AppSettings["WebAppVersion"];
            string requesturi = ConfigurationManager.AppSettings["requesturi"];
            string response = null;
            var jsonItem = JsonConvert.SerializeObject(item);
            Task.Run(
            async () =>
            {
                response = await HttpHelper.PostAsync(requesturi, jsonItem);
            }).Wait();

            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            return View(item);
        }
    }
}

