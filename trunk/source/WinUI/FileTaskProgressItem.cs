using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace FilesHashWUI
{
    public sealed class FileTaskProgressItem : INotifyPropertyChanged
    {
        private string m_fileName = string.Empty;
        private string m_algorithmText = string.Empty;
        private string m_statusText = string.Empty;
        private double m_progressValue = 0;
        private string m_progressText = "0%";
        private bool m_isIndeterminate = false;
        private Brush m_statusBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x6B, 0x72, 0x7C));

        public string FilePath { get; set; } = string.Empty;

        public ulong FileSize { get; set; }

        public bool IsCompleted { get; set; }

        public bool IsFailed { get; set; }

        public string FileName
        {
            get => m_fileName;
            set => SetField(ref m_fileName, value);
        }

        public string AlgorithmText
        {
            get => m_algorithmText;
            set => SetField(ref m_algorithmText, value);
        }

        public string StatusText
        {
            get => m_statusText;
            set => SetField(ref m_statusText, value);
        }

        public double ProgressValue
        {
            get => m_progressValue;
            set => SetField(ref m_progressValue, value);
        }

        public string ProgressText
        {
            get => m_progressText;
            set => SetField(ref m_progressText, value);
        }

        public bool IsIndeterminate
        {
            get => m_isIndeterminate;
            set => SetField(ref m_isIndeterminate, value);
        }

        public Brush StatusBrush
        {
            get => m_statusBrush;
            set => SetField(ref m_statusBrush, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
