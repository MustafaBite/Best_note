using System;

namespace NOT_VE_GÜNLÜK
{
    public class JournalEntry
    {
        public DateTime Date { get; set; }
        public string Title { get; set; } = "";
        public string? Content { get; set; }
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
