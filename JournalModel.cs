using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace NOT_VE_GÜNLÜK
{
    public enum JournalType
    {
        Journal,
        Plan
    }

    public class JournalModel
    {
        public string Title { get; set; } = "";
        public string? LastUpdated { get; set; }
        public string? Icon { get; set; } // Emoji or text icon
        
        [JsonIgnore]
        public SolidColorBrush BackgroundColor { get; set; } = Brushes.White;
        
        public string? UpdateTimeDescription { get; set; }
        public string SelectedTheme { get; set; } = "Light";
        public JournalType Type { get; set; } = JournalType.Journal;

        public List<JournalEntry> Entries { get; set; } = new List<JournalEntry>();
    }
}
