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
    [Route("~/api/importedfilesapi", Name = "ImportedFilesApiController")]

    public class ImportedFilesApiController : Controller
    {
        private AppDbContext _db;

        public ImportedFilesApiController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public object Get(DataSourceLoadOptions loadOptions)
        {

            var model = _db.ImportFileInfo.Select(s => new ImportFileInfo
            {
                Id = s.Id,
                UserId = $"{s.User.FirstName} {s.User.LastName}",
                ImportDate = s.ImportDate,
                DateFile = s.DateFile,
                FileName = s.FileName,
                ImportedRowCount=s.ImportedRowCount
            });


            return DataSourceLoader.Load(model, loadOptions);

        }

        [HttpGet]
        [Route("/api/importedfilesapi/download", Name = "Download")]
        public FileStreamResult Download([FromQuery]int fileId)
        {
            string archiveFileName = "";
            string fileName = "";

            var row = _db.ImportFileInfo.FirstOrDefault(s => s.Id == fileId);
            if (row != null)
            {
                archiveFileName = row.ArchiveFileName;
                fileName = row.FileName;
            }

            MemoryStream ms = new MemoryStream();
            FileStream fs = new FileStream(archiveFileName, FileMode.Open);
            fs.CopyTo(ms);
            ms.Position = 0;

            fs.Close();


            if ((new FileInfo(fileName)).Extension.ToLower() == "xml")
                return File(ms, "application/vnd.xml", fileName);
            else
                return File(ms, "application/vnd.ms-excel", fileName);
        }
    }

}
