namespace travelexpensemanagement.Models.EmployeePortal
{
    public class TimeSheet_Model
    {
        public Timesheet_Header Header { get; set; }
        public List<Timesheet_Detail> Detail { get; set; }
        public List<Timesheet_Attachment> Attachment { get; set; }
    }

    public class Timesheet_Header
    {
        public string? TaskDescription { get; set; }
        public string? TaskTitle { get; set; }
        public string? Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? AssignedToID { get; set; }
        public int? AssignedByID { get; set; }
        public int? CCTOID { get; set; }
        public int? BCCTOID { get; set; }
        public string? DURATION { get; set; }
        public string? AssignedToReply { get; set; }
        public string? AssignedByReply { get; set; }
        public string? Status { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_date { get; set; }
        public string? AssignedBy { get; set; }
        public string? DOCID { get; set; }
        public string? action { get; set; }

    }

    public class Timesheet_Detail
    {
        public int? ReplyID { get; set; }
        public string? AssignedToReply { get; set; }
        public string? AssignedByReply { get; set; }

    }

    public class Timesheet_Attachment
    {
        public int? AttachmentID { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }

    }
}