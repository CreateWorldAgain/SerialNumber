using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CitizenSerialInfo.Models
{
    public class ApplicationRole: IdentityRole
    {
        public int MoreInfoCount { get; set; } // кол-во запросов в сутки доп информации о серийнике
      
    }
}
