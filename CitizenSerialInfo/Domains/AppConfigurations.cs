using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CitizenSerialInfo.Domains
{
    public class AppConfigurations
    {
        public int Port { get; set; }
        public string Domain { get; set; }
        public string ImportedExcelArchive { get; set; }
        public string WatchMonitorigForImport { get; set; }
        public string WebSocketBaseUrl { get; set; }
    }
}
