using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace CitizenSerialInfo.Models.ViewModels
{
    public class ApproveUserViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Error { get; set; }
    
    }
}