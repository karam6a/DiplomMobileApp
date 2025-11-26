using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LogisticMobileApp.Services;
using LogisticMobileApp.Helpers;
using Microsoft.Extensions.DependencyInjection;


namespace LogisticMobileApp.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly ApiService _api;

    public RegisterViewModel(IServiceProvider services, ApiService api)
    {
        _services = services;
        _api = api;

        OpenScannerCommand = new AsyncRelayCommand(OpenScannerAsync);
        RegisterCommand = new AsyncRelayCommand(RegisterAsync);
        GoToLoginCommand = new AsyncRelayCommand(GoToLoginAsync);

    }

    [ObservableProperty]
    private string code;

    public IAsyncRelayCommand OpenScannerCommand { get; }
    public IAsyncRelayCommand RegisterCommand { get; }
    public IAsyncRelayCommand GoToLoginCommand { get; }

    private async Task OpenScannerAsync()
    {
        // Создаём ScannerPage через DI (чтобы зависимости ScannerPage тоже могли инжектиться)
        var scanner = _services.GetService<Pages.ScannerPage>() ?? ActivatorUtilities.CreateInstance<Pages.ScannerPage>(_services);

        // Открываем модально
        await Application.Current.MainPage.Navigation.PushModalAsync(scanner);

        try
        {
            var result = await scanner.ScanAsync();
            if (!string.IsNullOrWhiteSpace(result))
            {
                Code = result; // обновит Entry через биндинг
            }
        }
        catch (TaskCanceledException)
        {
            // отмена пользователем — игнорируем
        }
        catch (OperationCanceledException)
        {
            // тоже игнорируем
        }
    }

    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            await Application.Current.MainPage.DisplayAlert("Ошибка", "Введите код активации", "OK");
            return;
        }

        try
        {

            var deviceHelper = _services.GetRequiredService<IDeviceHelper>();

            string deviceName = deviceHelper.GetDeviceName();
            string deviceId = await deviceHelper.GetOrCreateDeviceIdentifierAsync();


            var request = new Models.ActivateRequest
            {
                Code = Code,
                Device_name = deviceName,
                Device_identifier = deviceId
            };

            await Application.Current.MainPage.DisplayAlert("Успех", deviceId, "OK");

            var response = await _api.ActivateAsync(request);

            // Сохраняем токены в SecureStorage
            await SecureStorage.SetAsync("access_token", response.access_token);
            await SecureStorage.SetAsync("refresh_token", response.refresh_token);
            await SecureStorage.SetAsync("expires_in", response.expires_in.ToString());

            await Application.Current.MainPage.DisplayAlert("Успех", "Устройство активировано" + deviceId, "OK");

            // Навигация на DashboardPage
            await Shell.Current.GoToAsync("//DashboardPage");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Ошибка", $"Регистрация не удалась: {ex.Message}", "OK");
        }
    }

    private async Task GoToLoginAsync()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
