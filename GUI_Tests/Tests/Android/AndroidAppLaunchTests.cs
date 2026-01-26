using GUI_Tests.Base;

namespace GUI_Tests.Tests.Android;

/// <summary>
/// Простые тесты запуска приложения на Android устройстве
/// </summary>
public class AndroidAppLaunchTests : AndroidTestBase
{
    [Fact]
    public void App_ShouldLaunch_OnAndroidDevice()
    {
        // Arrange & Act - приложение запускается в конструкторе базового класса
        
        // Assert - если мы дошли сюда без исключений, приложение запустилось
        Assert.NotNull(Driver);
        Assert.NotNull(Driver.SessionId);
        
        // Делаем скриншот для подтверждения
        TakeScreenshot("Android_AppLaunched");
        
        // Сохраняем структуру UI для анализа
        SavePageSource("Android_AppLaunched");
    }

    [Fact]
    public void App_ShouldDisplay_SomeContent()
    {
        // Ждем загрузки UI
        Thread.Sleep(3000);
        
        // Делаем скриншот текущего состояния
        TakeScreenshot("Android_CurrentScreen");
        
        // Сохраняем PageSource - это поможет понять какие элементы есть на экране
        SavePageSource("Android_CurrentScreen");
        
        // Проверяем что приложение отобразило что-то
        var pageSource = GetPageSource();
        Assert.False(string.IsNullOrEmpty(pageSource), "Должна быть какая-то UI иерархия");
    }
}
