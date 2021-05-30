using System.Collections.Generic;

namespace Inkluzitron.Data.Entities
{
    public class BdsmTestOrgResult : QuizResultBase
    {
        public string Link { get; set; }
        public List<BdsmTestOrgItem> Items { get; set; } = new ();
    }
}
