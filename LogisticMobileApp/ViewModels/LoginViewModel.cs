// File: ViewModels/LoginViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using LogisticMobileApp.Services;
using LogisticMobileApp.Helpers;

namespace LogisticMobileApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly IDeviceHelper _deviceHelper;

    public LoginViewModel(ApiService api, IDeviceHelper deviceHelper)
    {
        _api = api;
        _deviceHelper = deviceHelper;
        _ = TryAutoLoginAsync();
    }

    private async Task TryAutoLoginAsync()
    {
        await Task.Delay(400); // даём кубику покрутиться

        try
        {
            bool success = await _api.TryRefreshTokenAsync();

            if (success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await CommunityToolkit.Maui.Alerts.Toast
                        .Make("Автовход выполнен", CommunityToolkit.Maui.Core.ToastDuration.Short, 14)
                        .Show();

                    await Shell.Current.GoToAsync("//DashboardPage");
                });
            }
            else
            {
                await GoToRegistrationWithToast("Требуется повторная активация устройства");
            }
        }
        catch (Exception ex)
        {
            await GoToRegistrationWithToast($"Ошибка подключения: {ex.Message}");
        }
    }

    private async Task GoToRegistrationWithToast(string message = "Требуется повторная активация устройства")
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            // Чистим токены
            SecureStorage.Remove("access_token");
            SecureStorage.Remove("refresh_token");
            SecureStorage.Remove("expires_in");

            // Показываем красивый тост
            await CommunityToolkit.Maui.Alerts.Toast
                .Make(message, CommunityToolkit.Maui.Core.ToastDuration.Long, 16)
                .Show();

            // Небольшая пауза, чтобы пользователь успел прочитать
            await Task.Delay(1000);

            // Переход на страницу регистрации — правильный маршрут!
            await Shell.Current.GoToAsync("//RegisterPage");
        });
    }
}