using System.ComponentModel.DataAnnotations.Schema;

namespace HR_Application.Models
{
    public class Job
    {
        [Column("JOB_ID")]
        public string Id { get; set; } = string.Empty;

        [Column("JOB_TITLE")]
        public string JobTitle { get; set; } = string.Empty;

        [Column("MIN_SALARY")]
        public int? MinSalary{ get; set; }

        [Column("MAX_SALARY")]
        public int? MaxSalary { get; set; }
    }
}
