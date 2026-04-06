using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using System.Collections.Generic;
using System;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class OfferLetterMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;

        public OfferLetterMasterController(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        // ================= INDEX =================
        public IActionResult Index()
        {
            ViewBag.TemplateList = GetTemplateList();
            return View("~/Views/HRMS/OfferLetterMaster/Index.cshtml");
        }

        // ================= MODEL =================
        public class OfferLetterTemplate
        {
            public int TemplateId { get; set; }
            public string TemplateName { get; set; }
            public string TemplateBody { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        // ================= GET TEMPLATE LIST =================
        private List<OfferLetterTemplate> GetTemplateList()
        {
            List<OfferLetterTemplate> list = new();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand(
                    "SELECT TemplateId, TemplateName FROM OfferLetterTemplate WHERE IsActive = 1", con);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new OfferLetterTemplate
                    {
                        TemplateId = Convert.ToInt32(dr["TemplateId"]),
                        TemplateName = dr["TemplateName"].ToString()
                    });
                }
            }
            return list;
        }

        // ================= SAVE TEMPLATE =================
        [HttpPost]
        public IActionResult SaveTemplate(OfferLetterTemplate model)
        {
            // Ensure TemplateName is not empty or null
            if (string.IsNullOrEmpty(model.TemplateName))
            {
                return Json(new { success = false, message = "Template Name cannot be empty" });
            }

            bool templateExists = false;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                // Check if a template with the same name exists in the database
                SqlCommand checkCmd = new SqlCommand(@"
                    SELECT COUNT(1) 
                    FROM OfferLetterTemplate 
                    WHERE TemplateName = @TemplateName 
                    AND IsActive = 1", con);

                checkCmd.Parameters.AddWithValue("@TemplateName", model.TemplateName);
                con.Open();
                templateExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            }

            if (templateExists)
            {
                // If template exists, update the template
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand(@"
                        UPDATE OfferLetterTemplate
                        SET TemplateBody = @TemplateBody, 
                            CreatedDate = GETDATE() 
                        WHERE TemplateName = @TemplateName", con);

                    cmd.Parameters.AddWithValue("@TemplateName", model.TemplateName);
                    cmd.Parameters.AddWithValue("@TemplateBody", model.TemplateBody);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Template updated successfully" });
            }
            else
            {
                // If template does not exist, insert a new template
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO OfferLetterTemplate
                        (TemplateName, TemplateBody, IsActive, CreatedDate)
                        VALUES (@TemplateName, @TemplateBody, 1, GETDATE())", con);

                    cmd.Parameters.AddWithValue("@TemplateName", model.TemplateName);
                    cmd.Parameters.AddWithValue("@TemplateBody", model.TemplateBody);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Template saved successfully" });
            }
        }

        // ================= GET TEMPLATE BY ID =================
        [HttpGet]
        public IActionResult GetTemplateById(int templateId)
        {
            string body = "";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand(
                    "SELECT TemplateBody FROM OfferLetterTemplate WHERE TemplateId = @TemplateId", con);

                cmd.Parameters.AddWithValue("@TemplateId", templateId);

                con.Open();
                body = Convert.ToString(cmd.ExecuteScalar());
            }

            return Json(body);  // Return the template body as JSON
        }
    }
}
