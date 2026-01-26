using GUI_Tests.Base;

namespace GUI_Tests.Tests.Android;

/// <summary>
/// Тесты навигации и общего функционала приложения
/// </summary>
public class AndroidNavigationTests : AndroidTestBase
{
    [Fact]
    public void App_ShouldStart_WithoutCrash()
    {
        // Простейший тест - приложение запустилось без ошибок
        Assert.NotNull(Driver);
        Assert.NotNull(Driver.SessionId);
        
        TakeScreenshot("Navigation_AppStarted");
    }

    [Fact]
    public void App_ShouldHave_SomeVisibleContent()
    {
        // Ждем загрузки UI
        Thread.Sleep(2000);
        
        // Получаем иерархию UI
        var pageSource = GetPageSource();
        
        // Сохраняем для анализа
        SavePageSource("Navigation_VisibleContent");
        TakeScreenshot("Navigation_VisibleContent");
        
        // Проверяем что есть какой-то контент
        Assert.False(string.IsNullOrEmpty(pageSource), "Должен быть какой-то UI контент");
        Assert.True(pageSource.Length > 100, "UI иерархия должна содержать элементы");
    }

    [Fact]
    public void App_BackButton_ShouldWork()
    {
        // Ждем загрузки
        Thread.Sleep(2000);
        
        TakeScreenshot("Navigation_BeforeBack");
        
        // Нажимаем кнопку "Назад"
        PressBack();
        
        Thread.Sleep(1000);
        
        TakeScreenshot("Navigation_AfterBack");
        
        // Приложение не должно упасть
        Assert.NotNull(Driver.SessionId);
    }

    [Fact]
    public void App_ShouldHandle_ScreenRotation()
    {
        // Ждем загрузки
        Thread.Sleep(2000);
        
        TakeScreenshot("Navigation_Portrait");
        
        // Поворачиваем в ландшафт
        Driver.Orientation = OpenQA.Selenium.ScreenOrientation.Landscape;
        Thread.Sleep(1000);
        
        TakeScreenshot("Navigation_Landscape");
        
        // Возвращаем портрет
        Driver.Orientation = OpenQA.Selenium.ScreenOrientation.Portrait;
        Thread.Sleep(1000);
        
        TakeScreenshot("Navigation_BackToPortrait");
        
        // Приложение не должно упасть после поворота
        Assert.NotNull(Driver.SessionId);
    }
}
