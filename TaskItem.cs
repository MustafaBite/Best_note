namespace NOT_VE_GÜNLÜK
{
    public enum TaskStatus
    {
        NotStarted,
        HalfDone,
        Completed
    }

    public class TaskItem
    {
        public string Title { get; set; } = "";
        public TaskStatus Status { get; set; } = TaskStatus.NotStarted;
    }
}
