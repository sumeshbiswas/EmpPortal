namespace travelexpensemanagement.Models.Payroll.HRMS
{
    public class OfferLetterViewModel
    {
        public int? TemplateId { get; set; }
        public string? TemplateName { get; set; }
        public string? TemplateBody { get; set; }

        public string? EmployeeName { get; set; }
        public string? Designation { get; set; }
        public string? CompanyName { get; set; }
        public string? JoiningDate { get; set; }
        public string? Salary { get; set; }

        public string? FinalLetter { get; set; }
    }
}
