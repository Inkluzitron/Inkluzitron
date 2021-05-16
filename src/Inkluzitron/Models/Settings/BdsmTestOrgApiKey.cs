using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Models.Settings
{
    public class BdsmTestOrgApiKey
    {
        public string Uid { get; set; }
        public string Salt { get; set; }
        public string AuthSig { get; set; }
    }
}
