using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CitizenSerialInfo.Domains;
using CitizenSerialInfo.Models;
using CitizenSerialInfo.Services;
using static CitizenSerialInfo.Models.ImportFileInfo;

namespace CitizenSerialInfo.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private AppDbContext _db;
        private ILogger _logger;
        private UserManager<ApplicationUser> _userManager;
        private readonly IOptions<AppConfigurations> _appConfig;

        public HomeController(AppDbContext db, ILogger<HomeController> logger, UserManager<ApplicationUser> userManager,
            IOptions<AppConfigurations> appConfig)
        {
            _appConfig = appConfig;
            _db = db;
            _logger = logger;
            _userManager = userManager;
        } 

        public void Index()
        {
            Response.Redirect("/search");
        }    
        
        public void ChangeLang(string lang)
        {
            
        }
        
       
    }
}
