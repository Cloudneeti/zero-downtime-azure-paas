using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZeroDowntime.Core;
using Newtonsoft.Json;
using System.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;

namespace ZeroDowntime.WebApp.Controllers
{
    public class UserController : Controller
    {
        private string requestUri = ConfigurationManager.AppSettings["MiddleTierEndpoint"];
        private string ListUsersAPI = ConfigurationManager.AppSettings["ListUsersAPI"];
        private string UpsertUserAPI = ConfigurationManager.AppSettings["UpsertUserAPI"];
        private string dataVersion = ConfigurationManager.AppSettings["WebAppVersion"];
        private static RequestTelemetry telemetryRequest = new RequestTelemetry();
        private static TelemetryClient telemetryClient = new TelemetryClient()
        {
            InstrumentationKey = ConfigurationManager.AppSettings["APPINSIGHTS_INSTRUMENTATIONKEY"]
        };

        public UserController()
        {
            UserController.CreateTelemetryClient();
        }

        private static void CreateTelemetryClient()
        {
            telemetryRequest.GenerateOperationId();
            telemetryClient.Context.Operation.Id = telemetryRequest.Id;
            telemetryRequest.Context.Operation.Name = $"Application Version-{ConfigurationManager.AppSettings["WebAppVersion"]}";
        }

        // GET: Item
        [HttpGet]
        public ActionResult Index()
        {
            var requestStartTime = DateTime.UtcNow;
            //string data = string.Empty;

            //ViewBag.WebAppVersion = ConfigurationManager.AppSettings["WebAppVersion"];

            //string response=string.Empty;
            //Task.Run(
            //    async () =>
            //{
            //    response = await HttpHelper.GetAsync(requestUri);
            //}).Wait();

            //NBMEUser[] nbmeUsers = new NBMEUser[1];
            //if (string.IsNullOrEmpty(response))
            //{
            //    nbmeUsers = JsonConvert.DeserializeObject<NBMEUser[]>(response);
            //}
           
            telemetryClient.TrackRequest($"Application Version-{ConfigurationManager.AppSettings["WebAppVersion"]}", requestStartTime, DateTime.UtcNow - requestStartTime, "200", true);
            return List();
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(NBMEUser user)
        {
            user.DataVersion = dataVersion;

            Task.Run(
                async () =>
                {
                    await HttpHelper.PostAsync(UpsertUserAPI,
                        JsonConvert.SerializeObject(user));

                }).Wait();

            return RedirectToAction("List");
        }

        [HttpGet]
        public ActionResult Edit(string id)
        {
            string response = string.Empty;
            Task.Run(
                async () =>
                {
                    response = await HttpHelper.GetAsync(ListUsersAPI);
                }).Wait();

            var nbmeUsers = JsonConvert.DeserializeObject<NBMEUser[]>(response);
            var user = nbmeUsers.Where(u => u.Id == id).First();
            return View(user);
        }

        [HttpPost]
        public ActionResult Edit(NBMEUser user)
        {
            Task.Run(
                 async () =>
                 {
                     await HttpHelper.PostAsync(UpsertUserAPI, JsonConvert.SerializeObject(user));
                 }).Wait();

            return RedirectToAction("List");
        }

        public ActionResult List()
        {
            var requestStartTime = DateTime.UtcNow;

            ViewBag.WebAppVersion = ConfigurationManager.AppSettings["WebAppVersion"];

            string response = string.Empty;
            Task.Run(
                async () =>
                {
                    response = await HttpHelper.GetAsync(ListUsersAPI);
                }).Wait();

            NBMEUser[] nbmeUsers = null;
            if (!string.IsNullOrEmpty(response))
            {
                nbmeUsers = JsonConvert.DeserializeObject<NBMEUser[]>(response);
            }
            telemetryClient.TrackRequest($"Application Version-{ConfigurationManager.AppSettings["WebAppVersion"]}", requestStartTime, DateTime.UtcNow - requestStartTime, "200", true);
            return View(nbmeUsers.AsEnumerable());
        }

        //[HttpPost]
        //[ActionName("Create")]
        //[ValidateAntiForgeryToken]
        //public ActionResult CreateAsync([Bind(Include = "Id,Name,Description,Summary")] NBMEUser item)
        //{
        //    ViewBag.WebAppVersion = ConfigurationManager.AppSettings["WebAppVersion"];
        //    string requesturi = ConfigurationManager.AppSettings["requesturi"];
        //    string response = null;
        //    var jsonItem = JsonConvert.SerializeObject(item);
        //    Task.Run(
        //    async () =>
        //    {
        //        response = await HttpHelper.PostAsync(requesturi, jsonItem);
        //    }).Wait();

        //    if (ModelState.IsValid)
        //    {
        //        return RedirectToAction("Index");
        //    }

        //    return View(item);
        //}
    }
}

