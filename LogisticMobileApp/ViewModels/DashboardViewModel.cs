// ViewModels/DashboardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using LogisticMobileApp.Models;
using LogisticMobileApp.Services;

namespace LogisticMobileApp.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty]
    private string driverName = "Загрузка...";

    [ObservableProperty]
    private string driverPhone = "-";

    [ObservableProperty]
    private string driverStatus = "Загрузка...";

    [ObservableProperty]
    private Color driverStatusColor = Colors.Gray; // или #888888

    [ObservableProperty]
    private bool isLoading = true;

    [ObservableProperty]
    private bool errorMessageIsActive = false;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private DriverInfo driverInfo;

    public DashboardViewModel(ApiService api)
    {
        _api = api;
        LoadDriverInfoAsync();
    }

    private async void LoadDriverInfoAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var driver = await _api.GetCurrentDriverAsync();

            if (driver != null)
            {
                DriverName = driver.Name;
                DriverPhone = driver.Phone_number;

                DriverStatus = driver.Is_active ? "Активен" : "Неактивен";
                DriverStatusColor = driver.Is_active ? Colors.Green : Colors.Red;
            }
            else
            {
                errorMessageIsActive = true;
                ErrorMessage = "Данные водителя не получены";
            }
        }
        catch (Exception ex)
        {
            errorMessageIsActive = true;
            ErrorMessage = $"Ошибка: {ex.Message}";
            DriverName = "Не удалось загрузить";
        }
        finally
        {
            IsLoading = false;
        }
    }
}