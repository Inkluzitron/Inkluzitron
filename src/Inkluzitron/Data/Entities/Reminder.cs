using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkluzitron.Data.Entities
{
    public class Reminder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ReminderId { get; set; }

        [Required]
        public string MessageUrl { get; set; }

        [Required]
        public string Reason { get; set; }
    }
}
