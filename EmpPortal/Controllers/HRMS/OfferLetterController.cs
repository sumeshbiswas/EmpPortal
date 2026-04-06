using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.HRMS;
using static travelexpensemanagement.Controllers.Payroll.HRMS.OfferLetterMasterController;

namespace travelexpensemanagement.Controllers.Payroll.HRMS
{
    public class OfferLetterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly Controllers.DropdownService.DropdownService _dropdownService;
        private readonly DbHelper.DbHelper _dbHelper;
        private readonly ModuleService.ModuleService _moduleService;

        public OfferLetterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
        }
        // LOAD PAGE
        public IActionResult Index()
        {
            ViewBag.TemplateList = GetTemplateList();
            return View("~/Views/HRMS/OfferLetter/Index.cshtml");
        }

        public JsonResult GetEmployeeName()
        {
            string query = $@"Select distinct Code, USER_NAME From USER_MAST ORDER BY USER_NAME ASC";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlCompanyName()
        {
            string query = $@"Select Code, Name From COMP_MAST";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlFinalDesignation()
        {
            string query = $@"Select distinct Code, Name From DESG_MAST ORDER BY Name ASC";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
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

        [HttpGet]
        public IActionResult GetTemplateById(int templateId)
        {
            string body = "";
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand("SELECT TemplateBody FROM OfferLetterTemplate WHERE TemplateId = @TemplateId", con);
                cmd.Parameters.AddWithValue("@TemplateId", templateId);
                con.Open();
                body = Convert.ToString(cmd.ExecuteScalar());
            }
            return Json(body);
        }

        [HttpPost]
        public IActionResult SaveOfferLetter([FromBody] OfferLetterVM model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data" });
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand(@"
            INSERT INTO Hrms_OfferLetterDetails
            (TemplateId, COMP_CODE, EmployeeId, DesignationId, CompanyId, JoiningDate, Salary, OfferLetterBody)
            VALUES
            (@TemplateId, @COMP_CODE, @EmployeeId, @DesignationId, @CompanyId, @JoiningDate, @Salary, @OfferLetterBody)", con);

                cmd.Parameters.AddWithValue("@TemplateId", model.TemplateId);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@EmployeeId", model.EmployeeId);
                cmd.Parameters.AddWithValue("@DesignationId", model.DesignationId);
                cmd.Parameters.AddWithValue("@CompanyId", model.CompanyId);
                cmd.Parameters.AddWithValue("@JoiningDate", model.JoiningDate);
                cmd.Parameters.AddWithValue("@Salary", model.Salary);
                cmd.Parameters.AddWithValue("@OfferLetterBody", model.OfferLetterBody);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            return Json(new { success = true, message = "Offer Letter Saved Successfully" });
        }

        private void SendThankYouEmail(BusinessCards card)
        {
            string salutation = "Sir";

            if (!string.IsNullOrEmpty(card.Gender))
            {
                salutation = card.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase)
                    ? "Madam"
                    : "Sir";
            }

            string body = $@"
<html>
<body style='font-family: Arial, sans-serif; font-size:14px;'>

<p>Dear {salutation},</p>
<p><b>Kind attention to:</b> {card.Name}</p>

<p>
Thank you for visiting our stall at the <b>PLASTINDIA Exhibition</b>.<br/>
It was a pleasure to connect with you and share insights about our offerings.
</p>

<p>
We are pleased to inform you that <b>Pashupati Group</b> is actively engaged in providing reliable
and industry-focused solutions in the fields of
<b>Plastic Recycling</b> and <b>Packaging Solutions</b>.
</p>

<p>
<b>📍 Stall Details:</b><br/>
<b>Hall No.:</b> H4F<br/>
<b>Stall No.:</b> B15
</p>

<p>
<b>For any inquiries, please contact us at the address below :</b>
</p>

<p>
<b><u>For Packaging Products</u></b><br/>
PP/HDPE Woven Fabric & Bags | BOPP Bags | FIBC Bags<br/>
📧 <b>fabric@pashupatigrp.com</b>
</p>

<p>
<b><u>For Plastic Recycling Products</u></b><br/>
rPET Flakes, rPET Chips, rPET MasterBatch, rPSF, PP Fibre, rHDPE Granules,
rPP Granules, rLDPE Granules.<br/>
📧 <b>recycling@pashupatigrp.com</b>
</p>

<p>
<b><u>For Plastic Recycling Raw Material Supply</u></b><br/>
PET Bottles, PET & Polyester Waste, PP, HD, LD, LLDPE, PPCP Waste.<br/>
📧 <b>wastemgmt@pashupatigrp.com</b>
</p>

<p>
<b>Visit our website:</b><br/>
👉 <a href='https://www.pashupatigrp.com'><b>www.pashupatigrp.com</b></a>
</p>

<p style='color:gray; font-size:12px;'>
<b>Please note:</b> This email has been sent from noreply@pashupatigrp.com.
Replies to this email address are not monitored.
</p>

<p>
We look forward to the opportunity of working together and building a successful business relationship.
</p>

<p>
<b>Warm regards,</b><br/>
<b>Pashupati Group</b>
</p>

</body>
</html>";

            var mail = new MailMessage
            {
                From = new MailAddress("noreply@pashupatigrp.com", "Pashupati Group"),
                Subject = "Thank You for Visiting Us at PLASTINDIA Exhibition",
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(card.Email);

            var smtp = new SmtpClient("smtp-mail.outlook.com", 587)
            {
                Credentials = new NetworkCredential(
                    "noreply@pashupatigrp.com",
                    "Apple@213"   // ⚠️ move to config in production
                ),
                EnableSsl = true
            };

            smtp.Send(mail);
        }



        [HttpGet]
        public IActionResult Checkhrmstable(int code)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var result = new
            {
                basic = 0,
                personal = 0,
                education = 0,
                family = 0,
                reference = 0,
                work = 0,
                interview = 0,
                letterintent = 0,
                firstname = "",
                CheckInterviewData = ""
            };

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetHRMS_TabStatus", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Code", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            result = new
                            {
                                basic = Convert.ToInt32(dr["Basic"]),
                                personal = Convert.ToInt32(dr["Personal"]),
                                education = Convert.ToInt32(dr["Education"]),
                                family = Convert.ToInt32(dr["Family"]),
                                reference = Convert.ToInt32(dr["Reference"]),
                                work = Convert.ToInt32(dr["Work"]),
                                interview = Convert.ToInt32(dr["Interview"]),
                                letterintent = Convert.ToInt32(dr["LetterIntent"]),
                                firstname = dr["FirstName"]?.ToString() ?? "",
                                CheckInterviewData = dr["CheckInterviewData"]?.ToString() ?? ""
                            };
                        }
                    }
                }
            }
            return Json(result);
        }

    }
}

public class OfferLetterVM
{
    public int TemplateId { get; set; }
    public int EmployeeId { get; set; }
    public int DesignationId { get; set; }
    public int CompanyId { get; set; }
    public DateTime? JoiningDate { get; set; }
    public decimal Salary { get; set; }
    public string OfferLetterBody { get; set; }
}




public class BusinessCards
{
    public string Name { get; set; }
    public string Gender { get; set; }
    public string MobileNo { get; set; }
    public string Email { get; set; }
    public string Company { get; set; }
    public string Address { get; set; }
    public string TradeType { get; set; }
    public string UserFeedback { get; set; }
    public string ImageBase64 { get; set; }
    public string ImagePath { get; set; }
    public string OcrText { get; set; }
    public List<string> CustomerProducts { get; set; }
    public List<string> SupplierProducts { get; set; }
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    public string Website { get; set; }   // Add this field
}

