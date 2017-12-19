using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZeroDowntime.Models;
using Newtonsoft.Json;

namespace ZeroDowntime.WebApp.Controllers
{
    public class ItemController : Controller
    {
        // GET: Item
        public ActionResult Index()
        {
            string response = null;
            Task.Run(
                async()=> {
                    response = await HttpHelper.GetAsync("https://zdfunction.azurewebsites.net/api/CosmosDataAccessFunc",
                        "gkodjNHDszgk5KiR9bj3k3n0aGBbeKVWCur9WhO45pNawwg3WPrZXw==");
                }).Wait();

            Item[] items = JsonConvert.DeserializeObject<Item[]>(response);

            //var items = new Item[] { new Item {Id="01",Description="tewst",Name="asdsds" } };
            return View(items);
        }

        [ActionName("Create")]
        public ActionResult CreateAsync()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAsync([Bind(Include = "Id,Name,Description")] Item item)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            return View(item);
        }
    }
}

