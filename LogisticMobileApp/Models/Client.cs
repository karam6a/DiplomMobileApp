using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LogisticMobileApp.Models
{
    /// <summary>
    /// Элемент списка точек (клиентов) для маршрутов.
    /// Поддерживает выбор через чекбокс и уведомляет ViewModel об изменениях.
    /// </summary>
    public class ClientItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Recurrence { get; set; } = string.Empty;
        public int ContainerCount { get; set; }
        public DateTime StartDate { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }

        private bool _isSelected;
        /// <summary>
        /// Флаг выбора точки для маршрута.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Вызывается при изменении IsSelected (удобно для VM).
        /// </summary>
        public event EventHandler? SelectionChanged;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
