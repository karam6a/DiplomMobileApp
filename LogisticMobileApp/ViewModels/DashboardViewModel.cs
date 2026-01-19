// ViewModels/DashboardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using LogisticMobileApp.Models;
using LogisticMobileApp.Services;
using LogisticMobileApp.Resources.Strings;

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

    [ObservableProperty]
    private string licensePlate = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    private string distance = string.Empty;

    [ObservableProperty]
    private string duration = string.Empty;

    [ObservableProperty]
    private MyRouteInfo myRouteInfo;

    [ObservableProperty]
    private string geometryJson = string.Empty;

    [ObservableProperty]
    private bool isRouteStarted = false;

    [ObservableProperty]
    private string startButtonText = "Начать";

    [ObservableProperty]
    private bool isRouteLoading = true;

    [ObservableProperty]
    private bool isRouteLoaded = false;

    [ObservableProperty]
    private bool hasNoRoute = false;

    public DashboardViewModel(ApiService api)
    {
        _api = api;
        LoadDriverInfoAsync();
    }

    /// <summary>
    /// Перезагружает данные (вызывается при возврате на страницу)
    /// </summary>
    public async Task ReloadAsync()
    {
        await LoadDriverInfoInternalAsync();
    }

    private async void LoadDriverInfoAsync()
    {
        await LoadDriverInfoInternalAsync();
    }

    private async Task LoadDriverInfoInternalAsync()
    {
        IsLoading = true;
        IsRouteLoading = true;
        IsRouteLoaded = false;
        ErrorMessage = string.Empty;

        try
        {
            var driver = await _api.GetCurrentDriverAsync();

            if (driver != null)
            {
                DriverName = driver.Name;
                DriverPhone = driver.Phone_number;
                DriverStatus = driver.Is_active ? AppResources.DriverActiveLabel : AppResources.DriverInactiveLabel;
                DriverStatusColor = driver.Is_active ? Colors.Green : Colors.Red;
                IsLoading = false;

                // Загружаем маршрут для получения номера машины
                try
                {
                    var route = await _api.GetMyRouteAsync();
                    if (route != null && route.ClientsData != null && route.ClientsData.Count > 0)
                    {
                        LicensePlate = route.LicensePlate;
                        GeometryJson = route.GeometryJson;

                        //var nameParts = route.RouteName?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        //Name = nameParts != null && nameParts.Length > 1 ? nameParts[1] : route.RouteName ?? "";

                        Name = route.RouteName;
                        Status = route.Status;

                        Distance = (route.Distance / 1000).ToString("F1");
                        Duration = (route.Duration / 60).ToString("F0");

                        MyRouteInfo = route;

                        // Проверяем, начат ли маршрут
                        var routeStarted = Preferences.Get("RouteStarted", false);
                        var savedRouteId = Preferences.Get("RouteId", 0);
                        
                        if (routeStarted && savedRouteId == route.Id)
                        {
                            IsRouteStarted = true;
                            StartButtonText = AppResources.ContinueButtonText;
                        }
                        else
                        {
                            IsRouteStarted = false;
                            StartButtonText = AppResources.StartButtonText;
                        }

                        IsRouteLoaded = true;
                        HasNoRoute = false;
                    }
                    else
                    {
                        // Маршрут отсутствует
                        SetNoRouteState();
                    }
                }
                catch
                {
                    // Маршрут может отсутствовать - это нормально
                    SetNoRouteState();
                }
                finally
                {
                    IsRouteLoading = false;
                }
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
            IsRouteLoading = false;
        }
    }

    private void SetNoRouteState()
    {
        HasNoRoute = true;
        IsRouteLoaded = false;
        MyRouteInfo = null;
        Name = string.Empty;
        Distance = string.Empty;
        Duration = string.Empty;
    }
}