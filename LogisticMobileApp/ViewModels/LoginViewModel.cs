// File: ViewModels/LoginViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using LogisticMobileApp.Services;
using LogisticMobileApp.Helpers;

namespace LogisticMobileApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _api;

    public LoginViewModel(ApiService api)
    {
        _api = api;

        // ← ГАРАНТИРОВАННЫЙ запуск автологина
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(600); // чуть больше — чтобы кубик успел покрутиться
            await TryAutoLoginAsync();
        });
    }

    private async Task TryAutoLoginAsync()
    {
        await Task.Delay(400); // даём кубику покрутиться

        try
        {
            bool success = await _api.TryRefreshTokenAsync();

            if (success)
            {
                await ShowToastAndNavigate(
                    LocalizationResourceManager.Instance["Login_Success"],
                    "//DashboardPage");
            }
            else
            {
                // Любой сбой (нет сети, refresh не прошёл, сервер упал) — просто идём на регистрацию
                await GoToRegisterPageWithMessage(
                    LocalizationResourceManager.Instance["Login_AutoLoginFailed"]);
            }
        }
        catch
        {
            // Любая ошибка — тоже просто идём на регистрацию
            await GoToRegisterPageWithMessage(
                LocalizationResourceManager.Instance["Login_NetworkError"]);
        }
    }

    private async Task ShowToastAndNavigate(string message, string route)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await CommunityToolkit.Maui.Alerts.Toast
                .Make(message, CommunityToolkit.Maui.Core.ToastDuration.Short, 14)
                .Show();

            await Shell.Current.GoToAsync(route);
        });
    }

    private async Task GoToRegisterPageWithMessage(string message)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            // НИКОГДА НЕ ЧИСТИМ ТОКЕНЫ ЗДЕСЬ!
            // Они могут быть валидными, просто сейчас нет сети

            await CommunityToolkit.Maui.Alerts.Toast
                .Make(message, CommunityToolkit.Maui.Core.ToastDuration.Long, 16)
                .Show();

            await Task.Delay(1000);
            await Shell.Current.GoToAsync("//RegisterPage");
        });
    }
}