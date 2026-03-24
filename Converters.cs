using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NOT_VE_GÜNLÜK
{
    public class TaskStatusToEmojiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is TaskStatus status) || !(values[1] is DateTime currentDate))
                return ".";

            bool isPast = currentDate.Date < DateTime.Today;

            switch (status)
            {
                case TaskStatus.Completed:
                    return "✅";
                case TaskStatus.HalfDone:
                    return "⏳";
                case TaskStatus.NotStarted:
                    return isPast ? "❌" : "";
                default:
                    return "";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TaskStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TaskStatus status)) return Visibility.Collapsed;
            
            string? requested = parameter as string;
            if (requested == "Completed")
                return status == TaskStatus.Completed ? Visibility.Visible : Visibility.Collapsed;
            if (requested == "HalfDone")
                return status == TaskStatus.HalfDone ? Visibility.Visible : Visibility.Collapsed;
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
