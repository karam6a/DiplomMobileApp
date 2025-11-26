using LogisticMobileApp.ViewModels;

namespace LogisticMobileApp.Pages
{

    public partial class RoutesPage : ContentPage
    {
        private readonly RoutesViewModel _viewModel;
        public RoutesPage(RoutesViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is RoutesViewModel vm)
                await vm.LoadAsync();
        }
    }
}