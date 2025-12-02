using LogisticMobileApp.Helpers;

namespace LogisticMobileApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        UpdateMenuTitles();
        LocalizationResourceManager.Instance.PropertyChanged += (_, __) => UpdateMenuTitles();

    }

    private void UpdateMenuTitles()
    {
        var loc = LocalizationResourceManager.Instance;

        Resources["Menu_Login"] = loc["Login_Title"];
        Resources["Menu_Register"] = loc["Register_Title"];
        Resources["Menu_Dashboard"] = loc["Dashboard_Title"];
        Resources["Menu_Settings"] = loc["Settings_Title"];
        Resources["Menu_Routes"] = loc["Routes_Title"];
        Resources["Menu_Confirm"] = loc["ConfirmRoute_Title"];
    }
}

