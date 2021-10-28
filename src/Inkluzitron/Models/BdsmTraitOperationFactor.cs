using System.Collections.Generic;

namespace Inkluzitron.Models
{
    public class BdsmTraitOperationFactor
    {
        public double Score { get; set; }
        public double Weight { get; set; }
        public double Contribution => Score * Weight;
        public Dictionary<string, (string User, string Target)> Values { get; } = new();
    }
}
