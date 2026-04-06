using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EmpPortal.Models.Login
{
    public class LoginViewModel
    {
        public string UserMasterCode { get; set; }
        public string Password { get; set; }
        public string CompanyCode { get; set; }
        public string FinancialYear { get; set; }
        public DateTime LoginDate { get; set; }
    }
    public class RegisterUser
    {
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
    }


}
