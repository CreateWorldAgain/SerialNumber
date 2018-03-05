using System.ComponentModel.DataAnnotations;

namespace CitizenSerialInfo.Controllers
{
  
        public class ForgotPasswordViewModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }
    }
