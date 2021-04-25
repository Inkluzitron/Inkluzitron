using System;
using System.Collections.Generic;

namespace Inkluzitron.Data
{
    public class BaseTestResult
    {
        public Guid ResultId { get; set; }
        public DateTimeOffset SubmittedAt { get; set; }
        public ulong SubmittedById { get; set; }
        public string SubmittedByName { get; set; }
        public List<BaseTestResultItem> Items { get; set; } = new List<BaseTestResultItem>();
    }
}
