using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using CitizenSerialInfo.Models;

namespace CitizenSerialInfo.Controllers.api
{
    [Route("~/api/searchapi", Name = "SearchApiController")]

    public class SearchApiController : Controller
    {
        private AppDbContext _db;
        private UserManager<ApplicationUser> _userManager;

        public SearchApiController(AppDbContext db, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _db = db;
        }

        [HttpGet]
        public object Get(DataSourceLoadOptions loadOptions, string searchText)
        {

            IEnumerable<SerialInfo> model;

            if (User.IsInRole("Administrator"))
            {
                model = _db.SerialInfo.Where(s => s.SerialNumber.Contains(searchText) ||
                  s.Model.Contains(searchText) || s.Reference1.Contains(searchText) ||
                  s.Reference2.Contains(searchText));
            }
            else
            {
                if (User.IsInRole("Staff"))
                {
                    model = _db.SerialInfo.Where(s => s.SerialNumber.Equals(searchText) ||
                      s.Model.Contains(searchText) || s.Reference1.Contains(searchText) ||
                      s.Reference2.Contains(searchText));
                }

                else // default
                {
                    model = _db.SerialInfo.Where(s => s.SerialNumber.Equals(searchText) ||
                      s.Model.Equals(searchText) || s.Reference1.Contains(searchText) ||
                      s.Reference2.Contains(searchText));
                }
            }

            return DataSourceLoader.Load(model, loadOptions);
        }
      
        [HttpGet]
        [Route("/api/searchapi/requestmoreinfo", Name="RequestMoreInfo")]
        async public Task<object> RequestMoreInfo()
        {
            bool result = true;

            var user = (await _userManager.FindByNameAsync(User.Identity.Name));

            int moreInfoCount = user.MoreInfoCountUsed;
            DateTime moreInfoDate = user.MoreInfoDate;

            if ((new DateTime()).DayOfYear == moreInfoDate.DayOfYear)
            {
                moreInfoCount--;
                if (moreInfoCount <= 0)
                    result = false;
                else
                    result = true;

                user.MoreInfoCountUsed = moreInfoCount;
                _db.SaveChanges();
            }
            else
            {
                user.MoreInfoDate = new DateTime();
                user.MoreInfoCountUsed = user.MoreInfoCount - 1;

                _db.SaveChanges();
            }

            return Json(new { canRequestMoreInfo = result });
        }
    }


}
