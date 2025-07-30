using System.ComponentModel.DataAnnotations.Schema;

namespace HR_Application.Models
{
    public class Department
    {
     
            [Column("DEPARTMENT_ID")]
            public int Id { get; set; }
            [Column("DEPARTMENT_NAME")]
            public string DepartmentName { get; set; } = string.Empty;
        [Column("MANAGER_ID")]
            public int? ManagerId { get; set; }
            [Column("LOCATION_ID")]
            public int? LocationId { get; set; }
         
    }
}
