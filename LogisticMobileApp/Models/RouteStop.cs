using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LogisticMobileApp.Models
{
    public class RouteStop : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public int ContainerCount { get; set; }
        
        /// <summary>
        /// Оригинальный индекс точки в маршруте (для сохранения порядка нумерации)
        /// </summary>
        public int OriginalIndex { get; set; }

        private bool _isRejected;
        public bool IsRejected
        {
            get => _isRejected;
            set 
            { 
                if (_isRejected != value) 
                { 
                    _isRejected = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(IsPendingRejection));
                } 
            }
        }

        private bool _isConfirmed;
        public bool IsConfirmed
        {
            get => _isConfirmed;
            set { if (_isConfirmed != value) { _isConfirmed = value; OnPropertyChanged(); } }
        }

        private string _comment = "";
        public string Comment
        {
            get => _comment;
            set { if (_comment != value) { _comment = value; OnPropertyChanged(); } }
        }
        
        private bool _isCommentSent;
        /// <summary>
        /// Комментарий отправлен (точка полностью обработана как отклонённая)
        /// </summary>
        public bool IsCommentSent
        {
            get => _isCommentSent;
            set 
            { 
                if (_isCommentSent != value) 
                { 
                    _isCommentSent = value; 
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPendingRejection));
                } 
            }
        }
        
        /// <summary>
        /// Точка в состоянии ожидания ввода комментария (отклонена, но комментарий не отправлен)
        /// </summary>
        public bool IsPendingRejection => IsRejected && !IsCommentSent;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
