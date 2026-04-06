using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.WhatsupAPISetting
{
    public class WhatsupAPISettingController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        public WhatsupAPISettingController(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public JsonResult Savebtn(string SMSKey,string Sender,int? AutoSMSUser1,int? AutoSMSUser2,int? AutoSMSUser3,string Route,string URL,string WhatsupTokenID, string InstantID)
        {
            if (string.IsNullOrWhiteSpace(WhatsupTokenID) || string.IsNullOrWhiteSpace(InstantID))
            {
                return Json(new { success = false, message = "WhatsApp Token ID and Instant ID are required." });
            }
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // 1. Get the next SRNO
                    int nextSrno = 1;
                    string getMaxSrnoQuery = "SELECT ISNULL(MAX(SRNO), 0) + 1 FROM SYS_SMS";
                    using (SqlCommand getMaxCmd = new SqlCommand(getMaxSrnoQuery, con))
                    {
                        nextSrno = Convert.ToInt32(getMaxCmd.ExecuteScalar());
                    }
                    // 2. Insert new record
                    string insertQuery = @"
                INSERT INTO SYS_SMS (
                    SRNO, SMS_KEY, SMS_SENDER, SMS_USER1, SMS_USER2, SMS_USER3,
                    SMS_ROUTE, SMS_URL, WHATSUP_TOKENID, WHATSUP_INSTANTID, UDATE
                )
                VALUES (
                    @SRNO, @SMSKey, @Sender, @User1, @User2, @User3,
                    @Route, @URL, @WhatsupTokenID, @InstantID, GETDATE()
                )";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@SRNO", nextSrno);
                        cmd.Parameters.AddWithValue("@SMSKey", string.IsNullOrEmpty(SMSKey) ? (object)DBNull.Value : SMSKey);
                        cmd.Parameters.AddWithValue("@Sender", string.IsNullOrEmpty(Sender) ? (object)DBNull.Value : Sender);
                        cmd.Parameters.AddWithValue("@User1", AutoSMSUser1.HasValue ? (object)AutoSMSUser1.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@User2", AutoSMSUser2.HasValue ? (object)AutoSMSUser2.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@User3", AutoSMSUser3.HasValue ? (object)AutoSMSUser3.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Route", string.IsNullOrEmpty(Route) ? (object)DBNull.Value : Route);
                        cmd.Parameters.AddWithValue("@URL", string.IsNullOrEmpty(URL) ? (object)DBNull.Value : URL);
                        cmd.Parameters.AddWithValue("@WhatsupTokenID", WhatsupTokenID);
                        cmd.Parameters.AddWithValue("@InstantID", InstantID);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Details saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error occurred: " + ex.Message });
            }
        }



    }
}
