namespace travelexpensemanagement.Models.Payroll.HRMS
{
    //public class hrmsletterintentModel
    //{

    //    public int Code { get; set; }
    //    public string V_DATE { get; set; }
    //    public int EmployeeCode { get; set; }
    //    public int DepartmentCode { get; set; }
    //    public int DesignationCode { get; set; }
    //    public int ReportingManager { get; set; }
    //    public string ReportLocation { get; set; }

    //    public string DiscussionDate { get; set; }
    //    public string EffectiveDate { get; set; }
    //    public string AcceptanceDate { get; set; }
    //    public string JoiningDate { get; set; }

    //    public decimal TakeHomeSalary { get; set; }
    //    public decimal GrossSalary { get; set; }
    //}

    public class hrmsletterintentModel
    {
        public int Code { get; set; }
        public string V_DATE { get; set; }

        public string EmployeeCode { get; set; }
        public int DepartmentCode { get; set; }
        public int DesignationCode { get; set; }
        public int ReportingManager { get; set; }

        public string ReportLocation { get; set; }

        public string DiscussionDate { get; set; }
        public string EffectiveDate { get; set; }
        public string AcceptanceDate { get; set; }
        public string JoiningDate { get; set; }

        public decimal TakeHomeSalary { get; set; }
        public decimal GrossSalary { get; set; }
    }


}
