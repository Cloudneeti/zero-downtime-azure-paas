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
            var response = async()=> {
                await HttpHelper.GetAsync("https://zerodowntime.azurewebsites.net/api/CosmosDataAccessFunc", "wXhNlbWKGHXD/eowPMHWdm8aZL0zGCOCkSy/ABJJZGrUWEdTwzppkA==")};

            var item = JsonConvert.DeserializeObject<Item>(response);
            return View(item);
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

