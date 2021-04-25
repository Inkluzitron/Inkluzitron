using System;

namespace Inkluzitron.Data
{
    public class BaseTestResultItem
    {
        public Guid ItemId { get; set; }
        public BaseTestResult TestResult { get; set; }
        public string Key { get; set; }
    }
}
