using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CitizenSerialInfo.Models
{
    public class ImportFileInfo
    {
        [Key]
        public int Id { get; set; }

        
        public string UserId { get; set; }

        
        public string FileName { get; set; }

        
        public DateTime DateFile { get; set; }

        
        public DateTime ImportDate { get; set; }

        public int ImportedRowCount { get; set; }

        public string ArchiveFileName { get; set; }

        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<SerialInfo> SerialInfo { get; set; }


    }
}