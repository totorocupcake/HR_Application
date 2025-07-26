using System.ComponentModel.DataAnnotations.Schema;

namespace HR_Application.Models
{
    public class Employee
    {
        [Column("EMPLOYEE_ID")]
        public int Id { get; set; }
        [Column("FIRST_NAME")]
        public string? FirstName { get; set; }
        [Column("LAST_NAME")]
        public string LastName { get; set; } = string.Empty;
        [Column("EMAIL")]
        public string Email { get; set; } = string.Empty;
        [Column("PHONE_NUMBER")]
        public string? PhoneNumber { get; set; }
        [Column("HIRE_DATE")]
        public DateTime HireDate { get; set; }
        [Column("JOB_ID")]
        public string JobId { get; set; } = string.Empty;
        [Column("SALARY")]
        public double? Salary { get; set; }
        [Column("COMMISSION_PCT")]
        public double? CommissionPct { get; set; }
        [Column("MANAGER_ID")]
        public int? ManagerId { get; set; }
        [Column("DEPARTMENT_ID")]
        public int? DepartmentId { get; set; }
    }
}
