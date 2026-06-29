namespace AttendanceConnect.Models
{
    public class Employee
    {
        public string Pin { get; set; } = "";
        public string Name { get; set; } = "";
        public string Password { get; set; } = "";
        public int Privilege { get; set; }
        public string CardNumber { get; set; } = "";
    }
}
