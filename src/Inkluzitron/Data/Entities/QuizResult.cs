using System;
using System.Collections.Generic;

namespace Inkluzitron.Data.Entities
{
    public class QuizResult
    {
        public long ResultId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public ulong SubmittedById { get; set; }
        public string SubmittedByName { get; set; }
        public List<QuizItem> Items { get; set; } = new List<QuizItem>();
    }
}
