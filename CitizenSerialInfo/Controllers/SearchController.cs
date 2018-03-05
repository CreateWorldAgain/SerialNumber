using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CitizenSerialInfo.Models;

namespace CitizenSerialInfo.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.PortletTitle = "Search in Citizen database";
            ViewBag.ActiveMenuItem = "'#li-search'";

            return View();
        }    
                        
    }
}
