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
        private RequestTelemetry telemetryRequest=new RequestTelemetry();
        private TelemetryClient telemetryClient;

        public ItemController()
        {
            this.telemetryClient = new TelemetryClient() {
                InstrumentationKey = ConfigurationManager.AppSettings["APPINSIGHTS_INSTRUMENTATIONKEY"] };

            this.telemetryRequest.GenerateOperationId();
            this.telemetryClient.Context.Operation.Id = this.telemetryRequest.Id;
            this.telemetryRequest.Context.Operation.Name = $"GetItems-{ConfigurationManager.AppSettings["WebAppVersion"]}";
        }
        // GET: Item
        public ActionResult Index()
        {
            var requestStartTime = DateTime.UtcNow;
            string response = null;
            ViewBag.WebAppVersion = ConfigurationManager.AppSettings["WebAppVersion"];
            string requesturi = ConfigurationManager.AppSettings["MiddleTierEndpoint"];
            Task.Run(
                async()=> {
                    response = await HttpHelper.GetAsync(requesturi);
                }).Wait();

            Item[] items = new Item[] { new Item { Name = "Site1", Description = "nbme site", Summary = "test site" } };

            if(!string.IsNullOrEmpty(response) && !string.IsNullOrWhiteSpace(response))
            {
                items = JsonConvert.DeserializeObject<Item[]>(response);
            }

            this.telemetryClient.TrackRequest($"GetItems-{ConfigurationManager.AppSettings["WebAppVersion"]}", requestStartTime, DateTime.UtcNow - requestStartTime, "200", true);
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
           async () => {
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

