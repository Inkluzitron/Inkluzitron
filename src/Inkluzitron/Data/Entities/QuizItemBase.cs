using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkluzitron.Data.Entities
{
    public abstract class QuizItemBase<Result> where Result : QuizResultBase
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("ParentId")]
        public Result Parent { get; set; }
        public long ParentId { get; set; }
    }
}
