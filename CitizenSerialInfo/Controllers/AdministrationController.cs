using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CitizenSerialInfo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CitizenSerialInfo.Domains;
using CitizenSerialInfo.Models;
using Microsoft.AspNetCore.Http.Internal;

namespace CitizenSerialInfo.Controllers
{
    public class AdministrationController : Controller
    {
        private AppDbContext _db;
        private ILogger _logger;
        private UserManager<ApplicationUser> _userManager;
        private readonly IOptions<AppConfigurations> _appConfig;
        public AdministrationController(AppDbContext db, ILogger<AdministrationController> logger, UserManager<ApplicationUser> userManager,
                   IOptions<AppConfigurations> appConfig)
        {
            _appConfig = appConfig;
            _db = db;
            _logger = logger;
            _userManager = userManager;
        }
        
        [HttpGet]
        [Route("~/administration/users",Name="Users")]
        public IActionResult Users()
        {
            ViewBag.PortletTitle = "User reference";
            ViewBag.ActiveMenuItem = "'#li-users'";

            return View();
        }
        [HttpGet]
        [Route("~/administration/roles", Name = "Roles")]
        public IActionResult Roles()
        {
            ViewBag.PortletTitle = "Look Up per day";
            ViewBag.ActiveMenuItem = "'#li-users'";

            return View();
        }
        [HttpGet]
        [Route("~/administration/importdata", Name = "ImportData")]
        public IActionResult ImportData()
        {
            ViewBag.PortletTitle = "List imported files";
            ViewBag.ActiveMenuItem = "'#li-imported-data'";

            return View();
        }

        [HttpGet]
        [Route("~/administration/uploadfile", Name = "UploadFile")]
        public IActionResult UploadFile()
        {
            ViewBag.PortletTitle = "Upload Citizen Serial Info file to server";
            ViewBag.ActiveMenuItem = "'#li-upload'";

            var guid = Guid.NewGuid().ToString().ToLower();

            ViewBag.Guid = guid;
            ViewBag.WebSocketURL = $"{_appConfig.Value.WebSocketBaseUrl}?guid=" + guid;

            return View();
        }

        [HttpPost]
        [Route("~/administration/upload", Name = "Upload")]
        async public Task<IActionResult> Upload([FromQuery] string guid)
        {
            IFormFileCollection files = Request.Form.Files;
            
            string error = "";
            try
            {
                // full path to file in temp location
                var filePath = Path.GetTempFileName();
                     var user= await _userManager.FindByNameAsync(User.Identity.Name);
                foreach (var formFile in files)
                {
                    if (formFile.Length > 0)
                    {
                        var stream = new FileStream(filePath, FileMode.Create);

                        await formFile.CopyToAsync(stream);

                        stream.Close();


                        if (formFile.FileName.ToLower().EndsWith(".xml"))
                        {
                            error = ImportFile.ImportXml(filePath, formFile.FileName,  _db, _logger, _appConfig.Value.ImportedExcelArchive, user.Id, guid);
                        }
                        else if (formFile.FileName.ToLower().EndsWith(".xlsx"))
                        {
                            error = ImportFile.ImportExcel(filePath, formFile.FileName, _db, _logger, _appConfig.Value.ImportedExcelArchive, user.Id, guid);
                        }
                        else
                            error = "You can upoad only *.xml or *.xlsx files!!!";

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(Utils.GetFullError(ex));
                error = "Internal server error";
            }

            return Json(new { error=error});
        }
    }
}