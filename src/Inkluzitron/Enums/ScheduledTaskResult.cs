namespace Inkluzitron.Enums
{
    public enum ScheduledTaskResult
    {
        /// <summary>
        /// Returned by <see cref="Contracts.IScheduledTaskHandler.HandleAsync(Data.Entities.ScheduledTask)"/> when it does not understand the scheduled task instance it's given.
        /// </summary>
        NotHandled,

        /// <summary>
        /// Returned by <see cref="Contracts.IScheduledTaskHandler.HandleAsync(Data.Entities.ScheduledTask)"/> when it completed the given scheduled task and wants it removed from database.
        /// </summary>
        HandledAndCompleted,

        /// <summary>
        /// Returned by <see cref="Contracts.IScheduledTaskHandler.HandleAsync(Data.Entities.ScheduledTask)"/> when it completed the given scheduled task, adjusted its date/time and wants it saved back into database.
        /// </summary>
        HandledAndPostponed
    }
}
