namespace Inkluzitron.Data.Entities
{
    public class ReminderSubscription
    {
        public Reminder Reminder { get; set; }

        public ulong UserId { get; set; }
    }
}
