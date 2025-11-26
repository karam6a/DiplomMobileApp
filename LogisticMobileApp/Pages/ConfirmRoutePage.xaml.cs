using System.Windows.Input;
using Microsoft.Maui.Controls;
using LogisticMobileApp.ViewModels;
using LogisticMobileApp.Models;

namespace LogisticMobileApp.Pages
{
    public partial class ConfirmRoutePage : ContentPage
    {
        public ICommand ConfirmCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand SendCommentCommand { get; }

        public ConfirmRoutePage(IEnumerable<ClientItem> selected)
        {
            InitializeComponent(); // ← остаётся первым!

            ConfirmCommand = new Command<RouteStop>(stop =>
            {
                if (stop == null) return;
                stop.IsConfirmed = true;
                stop.IsRejected = false;
                stop.Comment = string.Empty;
            });

            RejectCommand = new Command<RouteStop>(stop =>
            {
                if (stop == null) return;
                stop.IsRejected = true;
                stop.IsConfirmed = false;
            });

            SendCommentCommand = new Command<RouteStop>(async stop =>
            {
                if (stop == null) return;
                await DisplayAlert("Комментарий отправлен",
                    $"{stop.Name}\n{(string.IsNullOrWhiteSpace(stop.Comment) ? "(пусто)" : stop.Comment)}",
                    "OK");
            });

            BindingContext = new ConfirmRouteViewModel(selected);
        }
    }


}
