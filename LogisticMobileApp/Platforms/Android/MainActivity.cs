using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace LogisticMobileApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Устанавливаем черный цвет для Status Bar (верхняя полоска)
        Window?.SetStatusBarColor(Android.Graphics.Color.Black);
        
        // Устанавливаем черный цвет для Navigation Bar (нижняя полоска с кнопками)
        Window?.SetNavigationBarColor(Android.Graphics.Color.Black);
        
        // Устанавливаем светлые иконки на системных панелях (для черного фона)
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            // Android 11+ (API 30+)
            Window?.InsetsController?.SetSystemBarsAppearance(0, (int)WindowInsetsControllerAppearance.LightStatusBars);
            Window?.InsetsController?.SetSystemBarsAppearance(0, (int)WindowInsetsControllerAppearance.LightNavigationBars);
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            // Android 6-10 (API 23-29) - убираем флаг светлого статус-бара для светлых иконок
            var decorView = Window?.DecorView;
            if (decorView != null)
            {
                decorView.SystemUiVisibility = (StatusBarVisibility)((int)decorView.SystemUiVisibility & ~(int)SystemUiFlags.LightStatusBar);
            }
        }
    }
}
