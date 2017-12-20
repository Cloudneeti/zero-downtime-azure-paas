using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZeroDowntime.Models;
using Newtonsoft.Json;
using System.Configuration;

namespace ZeroDowntime.WebApp.Controllers
{
    public class ItemController : Controller
    {
        // GET: Item
        public ActionResult Index()
        {
            string response = null;
            ViewBag.WebAppVersion = ConfigurationManager.AppSettings["WebAppVersion"];
            Task.Run(
                async()=> {
                    response = await HttpHelper.GetAsync("https://zdfunction.azurewebsites.net/api/CosmosDataAccessFunc","SvmMZbs8nmcGhlcO0vmG1a6LB9WSFRekMfFWyM6aP7AJQpv8SiD4CA==");
                }).Wait();

            Item[] items = JsonConvert.DeserializeObject<Item[]>(response);

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
        public ActionResult CreateAsync([Bind(Include = "Id,Name,Description")] Item item)
        {
            ViewBag.WebAppVersion = ConfigurationManager.AppSettings["WebAppVersion"];
            string response = null;
           var jsonItem = JsonConvert.SerializeObject(item);
           Task.Run(
           async () => {
               ////response = await HttpHelper.PostAsync("https://zdfunction.azurewebsites.net/api/CosmosDataAccessFunc",
               //    "gkodjNHDszgk5KiR9bj3k3n0aGBbeKVWCur9WhO45pNawwg3WPrZXw==", jsonItem);
               response = await HttpHelper.PostAsync("https://zdfunction.azurewebsites.net/api/CosmosDataAccessFunc","SvmMZbs8nmcGhlcO0vmG1a6LB9WSFRekMfFWyM6aP7AJQpv8SiD4CA==", jsonItem);
           }).Wait();

            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            return View(item);
        }
    }
}

