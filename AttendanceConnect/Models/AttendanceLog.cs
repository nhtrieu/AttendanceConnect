namespace AttendanceConnect.Models
{
    public class AttendanceLog
    {
        public int UserId { get; set; }
        public DateTime VerifyDate { get; set; }
        public int? VerifyType { get; set; }
        public int? VerifyState { get; set; }
        public int? WorkCode { get; set; }
        public string? DeviceCode { get; set; }
        public DateTime ImportedAt { get; set; }
    }
}
