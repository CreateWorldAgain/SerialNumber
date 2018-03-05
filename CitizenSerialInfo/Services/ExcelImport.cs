using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CitizenSerialInfo.Models;
using CitizenSerialInfo.Models.ViewModels;
using System.Threading;
using System.Net.WebSockets;

namespace CitizenSerialInfo.Services
{

    public class ImportFile
    {
        public static IFormatProvider ruFormatProvider = new CultureInfo("ru-RU");
        public static IFormatProvider usFormatProvider = new CultureInfo("en-US");

        public class SerialNumberRow
        {
            public String SerialNumber { get; set; }
            public String PartNumber { get; set; }
            public String Reference { get; set; }
            public String Reference2 { get; set; }
            public String Date { get; set; }
        }

        static public string ImportExcel(string fileName, string realFileName, AppDbContext db, ILogger logger, string archivePath, string userId, string guid)
        {
            string error = "";
            int rowNumber = 0; // номер строки в которой произошла ошибка при импорте

            FileInfo file = new FileInfo(fileName);

            if (File.Exists(fileName))
            {
                try
                {
                    ExcelPackage package = new ExcelPackage(file);

                    var fileInfo = new ImportFileInfo
                    {
                        FileName = realFileName,
                        ImportDate = DateTime.Now,
                        DateFile = File.GetLastWriteTime(fileName),
                        UserId = userId,
                        ImportedRowCount=0
                    };

                    db.ImportFileInfo.Add(fileInfo);

                    bool hasSecondSheet = (package.Workbook.Worksheets.Count > 1);

                    ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                    int rowCount = worksheet.Dimension.Rows;

                    ExcelWorksheet worksheet2 = null;
                    if (hasSecondSheet)
                        worksheet2 = package.Workbook.Worksheets[2];

                    int x = 0;

                    decimal oldPercent = -1;

                    bool sendToWebSocket = guid != null;
                    WebSocket websocket = null;

                    if (sendToWebSocket)
                        websocket = Startup.webSocketsList[guid];

                    for (int row = 2; row <= rowCount; row++)
                    {                        
                        if (sendToWebSocket)
                        {
                            var percent = Math.Truncate((100m * row) / rowCount);                            

                            var bin = new ArraySegment<byte>(Encoding.Default.GetBytes(percent.ToString()));

                            if (oldPercent != percent)
                            {
                                websocket.SendAsync(bin, System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                                oldPercent = percent;
                            }

                        }

                        rowNumber = row;

                        if (worksheet.Cells[row, 1].Value == null)
                            continue;

                        if (worksheet.Cells[row, 3].Value == null)
                            continue;

                        if (worksheet.Cells[row, 4].Value == null)
                            continue;

                        if (worksheet.Cells[row, 1].Value.ToString().Equals(""))
                            continue;

                        if (worksheet.Cells[row, 2].Value == null)
                            throw new Exception("Model/PartNumber is null");

                        var sn = worksheet.Cells[row, 1].Value.ToString();

                        string extendedWarrantyPeriod = "";
                        string dateExtensionStarted = "";
                        string actualModel = "";

                        if (hasSecondSheet)
                        {
                            //rowCount = worksheet2.Dimension.Rows;
                            var extendedData = worksheet2.Cells.FirstOrDefault(s => s.Text == sn);

                            if (extendedData != null)
                            {
                                int rowIndex = Convert.ToInt32(extendedData.Address.Substring(1));

                                if (worksheet2.Cells[rowIndex, 6].Value != null)
                                {
                                    extendedWarrantyPeriod = worksheet2.Cells[rowIndex, 6].Value.ToString();
                                }

                                if (worksheet2.Cells[rowIndex, 5].Value != null)
                                {
                                    dateExtensionStarted = worksheet2.Cells[rowIndex, 5].Value.ToString();
                                }

                                if (worksheet2.Cells[rowIndex, 2].Value != null)
                                {
                                    actualModel = worksheet2.Cells[rowIndex, 2].Value.ToString();
                                }

                            }
                        }

                        var serial = new SerialInfo
                        {
                            SerialNumber = sn,
                            Model = worksheet.Cells[row, 2].Value.ToString(),
                            Reference1 = worksheet.Cells[row, 3].Value.ToString(),
                            Reference2 = worksheet.Cells[row, 4].Value.ToString(),
                            Date = Convert.ToDateTime(worksheet.Cells[row, 5].Value),
                            ImportFileInfo = fileInfo,
                            ActualModel = actualModel,
                            DateExtensionStarted = dateExtensionStarted,
                            ExtendedWarrantyPeriod = extendedWarrantyPeriod
                        };


                        db.SerialInfo.Add(serial);
                        db.SaveChanges();
                        x++;
                    }

                    string archiveFileName = Guid.NewGuid().ToString().ToLower().Replace("-", "");
                    // кладем файл в папку с архивом
                    if (!archivePath.Equals(""))
                    {
                        if (!Directory.Exists(archivePath))
                        {
                            Directory.CreateDirectory(archivePath);
                        }

                        archiveFileName = Path.Combine(archivePath, archiveFileName+ (new FileInfo(realFileName)).Extension);

                        if (File.Exists(archiveFileName))
                            File.Delete(archiveFileName);

                        File.Move(fileName, archiveFileName);
                    }

                    fileInfo.ImportedRowCount = x;
                    fileInfo.ArchiveFileName = archiveFileName;

                    db.SaveChanges();

                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    error = $"Import error. Row: {rowNumber}. Error text: {ex.Message}";
                }
            }
            else
                error = $"File not exists {fileName}";

            return error;
        }
        static public string ImportXml(string fileName, string realFileName, AppDbContext db, ILogger logger, string archivePath, string userId, string guid)
        {
            string error = "";
            

            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("o", "urn:schemas-microsoft-com:office:office");
            nsmgr.AddNamespace("ss", "urn:schemas-microsoft-com:office:spreadsheet");
            nsmgr.AddNamespace("x", "urn:schemas-microsoft-com:office:excel");
            nsmgr.AddNamespace("html", @"http://www.w3.org/TR/REC-html40");
            nsmgr.AddNamespace("a", "urn:schemas-microsoft-com:office:spreadsheet");

            XmlElement root = doc.DocumentElement;

            XmlNodeList rows = doc.SelectNodes("//a:Workbook/a:Worksheet/a:Table/a:Row", nsmgr);

            List<string> refList = new List<string>();
            
            foreach(XmlElement row in rows)
            {
                var cellA = row.FirstChild.FirstChild.InnerText;
                var cellB = row.ChildNodes[1].FirstChild.InnerText;

                if (cellA.Equals("") || cellB.Equals(""))
                    break;
                else
                    refList.Add(cellB);
            }


            List<SerialNumberRow> serials = new List<SerialNumberRow>();
            foreach (var reference in refList)
            {
                string r = reference;

                if (r.StartsWith("N"))
                    r= r.Replace("N","SS");

                bool startCollectSerials = false;

                foreach (XmlElement row in rows)
                {
                    if (!startCollectSerials)
                    {
                        var cellA =(row.FirstChild.FirstChild==null)?"": row.FirstChild.FirstChild.InnerText;
                        if (cellA.Equals("Serial number"))
                        {
                            startCollectSerials = true;
                            continue;
                        }
                        else
                            continue;
                    }

                    if (!row.ChildNodes[0].FirstChild.InnerText.Equals(""))
                    {
                        serials.Add(new SerialNumberRow
                        {
                            SerialNumber = row.ChildNodes[0].FirstChild.InnerText,
                            PartNumber = row.ChildNodes[1].FirstChild.InnerText,
                            Reference = row.ChildNodes[2].FirstChild.InnerText,
                            Reference2 = row.ChildNodes[3].FirstChild.InnerText,
                            Date = row.ChildNodes[4].FirstChild.InnerText,
                        });                        
                    }
                }


            }

            var fileInfo = new ImportFileInfo
            {
                FileName = realFileName,
                ImportDate = DateTime.Now,
                DateFile = File.GetLastWriteTime(fileName),
                UserId = userId,
                ImportedRowCount= serials.Count
            };

            db.ImportFileInfo.Add(fileInfo);

            foreach (var sn in serials)
            {
                db.SerialInfo.Add(new SerialInfo
                {
                    Date = Convert.ToDateTime(sn.Date, ruFormatProvider),
                    ImportFileInfo = fileInfo,
                    Model = sn.PartNumber,
                    Reference1 = sn.Reference,
                    Reference2 = sn.Reference2,
                    SerialNumber = sn.SerialNumber,
                    ActualModel="",
                    DateExtensionStarted="",
                    ExtendedWarrantyPeriod=""

                });
            }

            string archiveFileName = Guid.NewGuid().ToString().ToLower().Replace("-", "");
            // кладем файл в папку с архивом
            if (!archivePath.Equals(""))
            {
                if (!Directory.Exists(archivePath))
                {
                    Directory.CreateDirectory(archivePath);
                }

                archiveFileName = Path.Combine(archivePath, archiveFileName + (new FileInfo(realFileName)).Extension);

                if (File.Exists(archiveFileName))
                    File.Delete(archiveFileName);

                File.Move(fileName, archiveFileName);
            }

            fileInfo.ArchiveFileName = archiveFileName;
            db.SaveChanges();

            return error;  
        }
    }
}
