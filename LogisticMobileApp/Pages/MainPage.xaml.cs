using System.Windows.Input;
using LogisticMobileApp.Resources;

namespace LogisticMobileApp.Pages;

public partial class MainPage : ContentPage
{
    public ICommand GoToRegisterCommand { get; }

    public MainPage()
    {
        InitializeComponent();
        GoToRegisterCommand = new Command(async () => await Shell.Current.GoToAsync("//RegisterPage"));
        BindingContext = this;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Здесь будет логика входа в будущем; пока переход на DashboardPage
        await Shell.Current.GoToAsync("//DashboardPage");
    }
}