using System;
using System.Collections.Generic;
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
    [Route("~/api/administrationapi",Name = "AdministrationApi")]

    public class AdministrationApiController : Controller
    {
        private class UserPasswords
        {
            public UserPasswords()
            {
                Password = "";
                ConfirmPassword = "";
            }

            public string Password { get; set; }
            public string ConfirmPassword { get; set; }
        }

        private AppDbContext _db;
        private UserManager<ApplicationUser> _userManager;
        private RoleManager<ApplicationRole> _roleManager;

        public AdministrationApiController(AppDbContext db, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        [HttpGet]
        public object Get(DataSourceLoadOptions loadOptions)
        {
            var model = _db.Users.ToList();
            var roles = _db.Roles.ToList();            
            foreach (var user in model)
            {
                user.Roles = new List<PermittedRole>();

                var userRoles = _db.UserRoles.Where(s => s.UserId == user.Id).ToList();

                foreach (var role in roles)
                {
                    if (userRoles.Any(s => s.RoleId == role.Id))
                        user.Roles.Add(new PermittedRole { Id = role.Id, RoleName = role.Name, IsPermitted = true, MoreCountInfo=role.MoreInfoCount });
                    else
                        user.Roles.Add(new PermittedRole { Id = role.Id, RoleName = role.Name, IsPermitted = false, MoreCountInfo = role.MoreInfoCount });
                }
            }

            return DataSourceLoader.Load(model, loadOptions);




        }

        [HttpPost]
        async public Task<IActionResult> Post(string values)
        {
            var newUser = new ApplicationUser();

            JsonConvert.PopulateObject(values, newUser);

            if (!TryValidateModel(newUser))
                return BadRequest(ModelState.GetFullErrorMessage());

            if (newUser.Password.Equals(""))
                return BadRequest("Check your password!");

            if (!newUser.Password.Equals(newUser.ConfirmPassword))
                return BadRequest("Password is not equals comnfirm password!");

            if (newUser.Roles==null || newUser.Roles.Count() == 0)
            {
                return BadRequest("You can not define user roles!");
            }

            String hashedNewPassword = _userManager.PasswordHasher.HashPassword(newUser, newUser.Password);
            newUser.PasswordHash = hashedNewPassword;

            _db.Users.Add(newUser);

            foreach(var role in newUser.Roles.Where(s=>s.IsPermitted==true))
            {
                int countAddInfo = (await _roleManager.FindByNameAsync(role.RoleName)).MoreInfoCount;

                newUser.MoreInfoCount = countAddInfo;

                var res = await _userManager.AddToRoleAsync(newUser, role.RoleName);
                if (!res.Succeeded)
                    return BadRequest($"Can not add user to role: {role}");
            }
            
            _db.SaveChanges();

            return Ok();
        }

        [HttpPut]
        async public Task<IActionResult> Put(string key, string values)
        {
            var user = _db.Users.First(a => a.Id == key);

            var oldRoles = _db.UserRoles.Where(s => s.UserId == key).ToList();

            JsonConvert.PopulateObject(values, user);
            var pwd = new UserPasswords();

            JsonConvert.PopulateObject(values, pwd);

            if (!TryValidateModel(user))
                return BadRequest(ModelState.GetFullErrorMessage());

            if (!pwd.Password.Equals(""))
            {
                if (!pwd.Password.Equals(pwd.ConfirmPassword))
                    return BadRequest("Password is not equal confirm password!");
                else
                {
                    String hashedNewPassword = _userManager.PasswordHasher.HashPassword(user, pwd.Password);
                    user.PasswordHash = hashedNewPassword;
                }
            }

            foreach(var role in oldRoles)
            {
                _db.UserRoles.Remove(role);
            }

            _db.SaveChanges();

            if (user.Roles != null)
            {
                foreach (var role in _db.Roles.ToList())
                {
                    if (user.Roles.Any(s => s.Id == role.Id))
                    {
                        var res = await _userManager.AddToRoleAsync(user, role.Name);


                    }
                }
            }

            _db.SaveChanges();

            return Ok();
        }

        [HttpDelete]
        public void Delete(string key)
        {
            var userRoles = _db.UserRoles.Where(s => s.UserId == key).ToList();
            _db.UserRoles.RemoveRange(userRoles);

            _db.Users.Remove(_db.Users.Find(key));

            _db.SaveChanges();
        }
    }

}
