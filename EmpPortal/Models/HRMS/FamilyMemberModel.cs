namespace travelexpensemanagement.Models.Payroll.HRMS
{
    public class FamilyMemberModel
    {
        public string Code { get; set; }
        public string FamilyMember { get; set; }
        public string Relationship { get; set; }
        public string Gender { get; set; }
        public int? Age { get; set; }
        public string Occupation { get; set; }
        public string Designation { get; set; }
        public string Address { get; set; }
        public string ContactNo { get; set; }
        public string Minor { get; set; }
        public string Nominee { get; set; }
        public decimal? Share { get; set; }
        public string Remarks { get; set; }
    }

    public class FamilyWrapperModel
    {
        public int Code { get; set; }
        public List<FamilyMemberModel> FamilyList { get; set; }
    }

    //public class FamilyDataWrapper
    //{
    //    public int Code { get; set; }
    //    public string Action { get; set; }
    //    public List<FamilyMemberModel> FamilyList { get; set; }
    //}

}
