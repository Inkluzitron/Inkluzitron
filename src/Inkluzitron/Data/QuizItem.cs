using System;

namespace Inkluzitron.Data
{
    public class QuizItem
    {
        public ulong ItemId { get; set; }
        public QuizResult Parent { get; set; }
        public string Key { get; set; }
    }
}
