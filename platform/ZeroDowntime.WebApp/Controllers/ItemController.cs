using System;
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
            var response = HttpHelper.GetAsync("https://zdfunction.azurewebsites.net/api/CosmosDataAccessFunc", "gkodjNHDszgk5KiR9bj3k3n0aGBbeKVWCur9WhO45pNawwg3WPrZXw==").Result;

            Item[] items = JsonConvert.DeserializeObject<Item[]>(response);
            return View();
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

