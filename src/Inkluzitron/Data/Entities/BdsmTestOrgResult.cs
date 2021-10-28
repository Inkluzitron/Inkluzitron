using Inkluzitron.Enums;
using System.Linq;
using System.Collections.Generic;

namespace Inkluzitron.Data.Entities
{
    public class BdsmTestOrgResult : QuizResultBase
    {
        public string Link { get; set; }
        public List<BdsmTestOrgItem> Items { get; set; } = new ();

        public double this[BdsmTrait trait]
        {
            get => Items.FirstOrDefault(i => i.Trait == trait)?.Score ?? 0;
        }
    }
}
