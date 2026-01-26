using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace GUI_Tests.Base;

/// <summary>
/// Базовый класс для всех Appium UI тестов
/// </summary>
public abstract class AppiumTestBase : IDisposable
{
    protected WindowsDriver Driver { get; private set; } = null!;
    
    // Путь к вашему скомпилированному приложению
    // Убедитесь, что приложение собрано для Windows
    private const string AppPath = @"C:\Users\User\source\repos\LogisticMobileApp\LogisticMobileApp\bin\Debug\net9.0-windows10.0.19041.0\win10-x64\LogisticMobileApp.exe";
    
    // URL WinAppDriver - убедитесь что WinAppDriver запущен!
    private const string WinAppDriverUrl = "http://127.0.0.1:4723/";

    protected AppiumTestBase()
    {
        InitializeDriver();
    }

    private void InitializeDriver()
    {
        var options = new AppiumOptions
        {
            PlatformName = "Windows",
            AutomationName = "Windows"
        };
        
        options.AddAdditionalAppiumOption("app", AppPath);
        options.AddAdditionalAppiumOption("deviceName", "WindowsPC");
        
        // Таймаут запуска приложения (в миллисекундах)
        options.AddAdditionalAppiumOption("ms:waitForAppLaunch", "10");
        
        Driver = new WindowsDriver(new Uri(WinAppDriverUrl), options);
        
        // Неявное ожидание элементов
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Найти элемент по AutomationId (x:Name в XAML)
    /// </summary>
    protected AppiumElement FindByAutomationId(string automationId)
    {
        return Driver.FindElement(MobileBy.AccessibilityId(automationId));
    }

    /// <summary>
    /// Найти элемент по имени (Name/Text)
    /// </summary>
    protected AppiumElement FindByName(string name)
    {
        return Driver.FindElement(MobileBy.Name(name));
    }

    /// <summary>
    /// Найти элемент по XPath
    /// </summary>
    protected AppiumElement FindByXPath(string xpath)
    {
        return Driver.FindElement(By.XPath(xpath));
    }

    /// <summary>
    /// Проверить существование элемента
    /// </summary>
    protected bool ElementExists(string automationId, int timeoutSeconds = 5)
    {
        try
        {
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(timeoutSeconds);
            var element = FindByAutomationId(automationId);
            return element.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
        finally
        {
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }
    }

    /// <summary>
    /// Подождать пока элемент станет видимым
    /// </summary>
    protected AppiumElement WaitForElement(string automationId, int timeoutSeconds = 10)
    {
        var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
        return (AppiumElement)wait.Until(d => d.FindElement(MobileBy.AccessibilityId(automationId)));
    }

    /// <summary>
    /// Сделать скриншот для отладки
    /// </summary>
    protected void TakeScreenshot(string name)
    {
        var screenshot = Driver.GetScreenshot();
        var path = Path.Combine(AppContext.BaseDirectory, $"Screenshots");
        Directory.CreateDirectory(path);
        screenshot.SaveAsFile(Path.Combine(path, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png"));
    }

    public void Dispose()
    {
        Driver?.Quit();
        Driver?.Dispose();
        GC.SuppressFinalize(this);
    }
}
