using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CitizenSerialInfo.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Obligatory field")]
        [Display(Name = "Login")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Obligatory field")]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "RememberMe")]
        public bool RememberMe { get; set; }
    }
}
