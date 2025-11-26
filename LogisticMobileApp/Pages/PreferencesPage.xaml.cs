using LogisticMobileApp.Helpers;
using Microsoft.Maui.Storage;
using System.Globalization;

namespace LogisticMobileApp.Pages;

public partial class PreferencesPage : ContentPage
{
    private readonly LocalizationResourceManager _localization;

    public PreferencesPage()
    {
        InitializeComponent();
        _localization = LocalizationResourceManager.Instance;
        BindingContext = _localization;
    }

    // Кнопка выбора языка – показываем ActionSheet с локализованными подписями
    private async void OnLanguageButtonClicked(object sender, EventArgs e)
    {
        // Локализованные строки из resx:
        // Language_PromptTitle, Language_Cancel
        var title = _localization["Language_PromptTitle"];
        var cancel = _localization["Language_Cancel"];

        var choice = await DisplayActionSheet(
            title,          // заголовок
            cancel,         // кнопка отмены
            null,           // destructive
            "ru", "en", "pl");

        if (string.IsNullOrEmpty(choice) || choice == cancel)
            return;

        try
        {
            var culture = new CultureInfo(choice);
            _localization.SetCulture(culture);
            Preferences.Set("Language", choice);
        }
        catch (Exception ex)
        {
            // Settings_Title и Settings_PasswordMismatch можно использовать для заголовков/сообщений
            await DisplayAlert(_localization["Settings_Title"],
                               $"{_localization["Settings_PasswordMismatch"]}\n{ex.Message}",
                               "OK");
        }
    }

    // Переключение светлой/тёмной темы
    private void OnChangeThemeClicked(object sender, EventArgs e)
    {
        var newTheme = Application.Current.UserAppTheme == AppTheme.Dark
            ? AppTheme.Light
            : AppTheme.Dark;

        Application.Current.UserAppTheme = newTheme;
        Preferences.Set("Theme", newTheme.ToString());
    }

    // Заготовка смены пароля с локализованными строками
    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        // Settings_ChangePassword, Settings_NewPassword, Settings_ConfirmPassword,
        // Settings_PasswordMismatch, Settings_PasswordConfirmed
        var newPass = await DisplayPromptAsync(
            _localization["Settings_ChangePassword"],
            _localization["Settings_NewPassword"],
            _localization["Settings_ConfirmPassword"],   // текст кнопки подтверждения
            _localization["Language_Cancel"],           // кнопка «Назад/Отмена»
            maxLength: 100,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(newPass))
            return;

        var confirm = await DisplayPromptAsync(
            _localization["Settings_ChangePassword"],
            _localization["Settings_ConfirmPassword"],
            _localization["Settings_ConfirmPassword"],
            _localization["Language_Cancel"],
            maxLength: 100,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(confirm))
            return;

        if (confirm == newPass)
        {
            await DisplayAlert(_localization["Settings_Title"],
                               _localization["Settings_PasswordConfirmed"],
                               "OK");
        }
        else
        {
            await DisplayAlert(_localization["Settings_Title"],
                               _localization["Settings_PasswordMismatch"],
                               "OK");
        }
    }
}
