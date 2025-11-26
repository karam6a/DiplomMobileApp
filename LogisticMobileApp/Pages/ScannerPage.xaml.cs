using ZXing.Net.Maui;
using System.Threading.Tasks;

namespace LogisticMobileApp.Pages;

public partial class ScannerPage : ContentPage
{
    private readonly TaskCompletionSource<string> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ScannerPage()
    {
        InitializeComponent();

        cameraView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };
    }

    // Возвращаем Task, который завершится при получении результата
    public Task<string> ScanAsync() => _tcs.Task;

    private void CameraView_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var first = e.Results?.FirstOrDefault();
        if (first == null) return;

        Dispatcher.DispatchAsync(async () =>
        {
            _tcs.TrySetResult(first.Value);
            await Navigation.PopModalAsync();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _tcs.TrySetCanceled(); // если пользователь закрыл без сканирования
    }
}
