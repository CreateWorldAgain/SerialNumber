using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CitizenSerialInfo.Models
{
    public class PermittedRole
    {
        public string Id { get; set; }
        public string RoleName { get; set; }
        public bool IsPermitted { get; set; }
        public int MoreCountInfo { get; set; }
    }

    public class ApplicationUser: IdentityUser
    {        
        [MaxLength(50)]
        public String FirstName { get; set; }

        [MaxLength(50)]
        public String LastName { get; set; }

        public bool IsApproved { get; set; } // подтвердил менеджер или нет

        public int MoreInfoCount { get; set; } // кол-во запросов в сутки доп информации о серийнике

        public DateTime MoreInfoDate { get; set; } // дата для счетчика
        public int MoreInfoCountUsed { get; set; } 

        [NotMapped]
        public string Password { get; set; }
        [NotMapped]
        public string ConfirmPassword { get; set; }
        [Required]
        [NotMapped]
        public List<PermittedRole> Roles { get; set; }

        public virtual ICollection<ImportFileInfo> ImportFileInfo { get; set; }
    }
}
