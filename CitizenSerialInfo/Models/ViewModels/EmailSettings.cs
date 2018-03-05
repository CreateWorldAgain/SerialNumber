using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CitizenSerialInfo.Models.ViewModels
{
    public class ConfirmEmailViewModel
    {
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public String ConfirmLink { get; set; }
        public String Email { get; set; }
    }

}
