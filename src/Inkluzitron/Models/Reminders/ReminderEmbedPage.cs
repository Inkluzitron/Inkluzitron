using Inkluzitron.Data.Entities;

namespace Inkluzitron.Models.Reminders
{
    public class ReminderEmbedPage
    {
        public int PageNumber { get; set; }
        public int PageCount { get; set; }
        public bool IsEmpty => Reminder == null;
        public Reminder Reminder { get; set; }
    }
}
