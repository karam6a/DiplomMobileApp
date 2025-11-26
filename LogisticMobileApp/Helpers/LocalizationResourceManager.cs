
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using LogisticMobileApp.Resources.Strings; 

namespace LogisticMobileApp.Helpers
{
    public class LocalizationResourceManager : INotifyPropertyChanged
    {
        public static LocalizationResourceManager Instance { get; } = new();

        private readonly ResourceManager _resourceManager = AppResources.ResourceManager;
        private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string this[string text]
            => _resourceManager.GetString(text, _currentCulture) ?? text;

        public CultureInfo CurrentCulture => _currentCulture;

        public void SetCulture(CultureInfo culture)
        {
            if (culture == null) return;
            if (_currentCulture.Name == culture.Name) return;

            _currentCulture = culture;

            
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}
