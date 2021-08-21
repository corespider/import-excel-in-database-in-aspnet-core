using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MVCCore.ImportExcel.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MVCCore.ImportExcel.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IWebHostEnvironment Environment;
        private IConfiguration Configuration;
        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment _environment, IConfiguration _configuration)
        {
            _logger = logger;
            Environment = _environment;
            Configuration = _configuration;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(IFormFile postedFile)
        {
            if (postedFile != null)
            {
                //Create a Folder.
                string path = Path.Combine(this.Environment.WebRootPath, "Uploads");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                //Save the uploaded Excel file.
                string fileName = Path.GetFileName(postedFile.FileName);
                string filePath = Path.Combine(path, fileName);
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }

                //Read the connection string for the Excel file.
                string conString = this.Configuration.GetConnectionString("ExcelConString");
                DataTable dt = new DataTable();
                conString = string.Format(conString, filePath);

                using (OleDbConnection connExcel = new OleDbConnection(conString))
                {
                    using (OleDbCommand cmdExcel = new OleDbCommand())
                    {
                        using (OleDbDataAdapter odaExcel = new OleDbDataAdapter())
                        {
                            cmdExcel.Connection = connExcel;

                            //Get the name of First Sheet.
                            connExcel.Open();
                            DataTable dtExcelSchema;
                            dtExcelSchema = connExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                            string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                            connExcel.Close();

                            //Read Data from First Sheet.
                            connExcel.Open();
                            cmdExcel.CommandText = "SELECT * From [" + sheetName + "]";
                            odaExcel.SelectCommand = cmdExcel;
                            odaExcel.Fill(dt);
                            connExcel.Close();
                        }
                    }
                }

                //Insert the Data read from the Excel file to Database Table.
                conString = this.Configuration.GetConnectionString("DBConnection");
                using (SqlConnection con = new SqlConnection(conString))
                {
                    using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(con))
                    {
                        //Set the database table name.
                        sqlBulkCopy.DestinationTableName = "dbo.customersDB";

                        //[OPTIONAL]: Map the Excel columns with that of the database table.
                        sqlBulkCopy.ColumnMappings.Add("CustomerId", "customer_id");
                        sqlBulkCopy.ColumnMappings.Add("FirstName", "first_name");
                        sqlBulkCopy.ColumnMappings.Add("LastName", "last_name");
                        sqlBulkCopy.ColumnMappings.Add("Phone", "phone");
                        sqlBulkCopy.ColumnMappings.Add("Email", "email");
                        sqlBulkCopy.ColumnMappings.Add("Street", "street");
                        sqlBulkCopy.ColumnMappings.Add("City", "city");
                        sqlBulkCopy.ColumnMappings.Add("State", "state");
                        sqlBulkCopy.ColumnMappings.Add("Zip", "zip_code");

                        con.Open();
                        sqlBulkCopy.WriteToServer(dt);
                        con.Close();
                    }
                }
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
