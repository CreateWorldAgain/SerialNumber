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
    [Route("~/api/moreinfoapi", Name = "MoreInfoApiController")]

    public class MoreInfoApiController : Controller
    {
        private AppDbContext _db;

        public MoreInfoApiController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public object Get(DataSourceLoadOptions loadOptions)
        {

            var model = _db.Roles.Select(s =>new
            {
                Id=s.Id,
                Name=s.Name,
                MoreInfoCount = s.MoreInfoCount
            });


            return DataSourceLoader.Load(model, loadOptions);

        }

        [HttpPut]
        public IActionResult Put(string key, string values)
        {
            var role = _db.Roles.First(a => a.Id == key);

            JsonConvert.PopulateObject(values, role);

            if (!TryValidateModel(role))
                return BadRequest(ModelState.GetFullErrorMessage());

            
            _db.SaveChanges();
            
            return Ok();
        }
    }

}
