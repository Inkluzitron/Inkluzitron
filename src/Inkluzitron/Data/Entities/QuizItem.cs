namespace Inkluzitron.Data.Entities
{
    public class QuizItem
    {
        public ulong ItemId { get; set; }
        public QuizResult Parent { get; set; }
        public string Key { get; set; }
    }
}
