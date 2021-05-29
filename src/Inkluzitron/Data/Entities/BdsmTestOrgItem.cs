using Inkluzitron.Enums;

namespace Inkluzitron.Data.Entities
{
    public class BdsmTestOrgItem : QuizItemBase<BdsmTestOrgResult>
    {
        public BdsmTrait Trait { get; set; }
        public double Score { get; set; }
    }
}
