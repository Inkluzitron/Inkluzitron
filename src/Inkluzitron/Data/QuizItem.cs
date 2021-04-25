using System;

namespace Inkluzitron.Data
{
    public class QuizItem
    {
        public Guid ItemId { get; set; }
        public QuizResult Parent { get; set; }
        public string Key { get; set; }
    }
}
