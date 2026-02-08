using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;

namespace GUI_Tests.Base;

/// <summary>
/// Базовый класс для Appium UI тестов на Android устройстве
/// </summary>
public abstract class AndroidTestBase : IDisposable
{
    protected AndroidDriver Driver { get; private set; } = null!;
    
    // URL Appium сервера
    private const string AppiumServerUrl = "http://127.0.0.1:4723/";
    

    private const string AppPackage = "com.companyname.logisticmobileapp";
    private const string AppActivity = "crc64e1fb321c08285b90.MainActivity";

    protected AndroidTestBase()
    {
        InitializeDriver();
    }

    private void InitializeDriver()
    {
        var options = new AppiumOptions
        {
            PlatformName = "Android",
            AutomationName = "UiAutomator2"
        };

        options.AddAdditionalAppiumOption("appPackage", AppPackage);
        options.AddAdditionalAppiumOption("appActivity", AppActivity);
        options.AddAdditionalAppiumOption("noReset", true);
        options.AddAdditionalAppiumOption("appWaitDuration", 30000);
        options.AddAdditionalAppiumOption("unicodeKeyboard", false);
        options.AddAdditionalAppiumOption("resetKeyboard", false);
        options.AddAdditionalAppiumOption("ignoreHiddenApiPolicyError", true);
        options.AddAdditionalAppiumOption("skipServerInstallation", false);
        options.AddAdditionalAppiumOption("suppressKillServer", true); 
        Driver = new AndroidDriver(new Uri(AppiumServerUrl), options);
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Найти элемент по AutomationId (resource-id в Android)
    /// В MAUI это content-desc или resource-id
    /// </summary>
    protected AppiumElement FindByAutomationId(string automationId)
    {
        // В MAUI на Android x:Name мапится в content-desc (AccessibilityId)
        return Driver.FindElement(MobileBy.AccessibilityId(automationId));
    }

    /// <summary>
    /// Найти элемент по resource-id (Android)
    /// </summary>
    protected AppiumElement FindByResourceId(string resourceId)
    {
        return Driver.FindElement(MobileBy.Id(resourceId));
    }

    /// <summary>
    /// Найти элемент по тексту
    /// </summary>
    protected AppiumElement FindByText(string text)
    {
        return Driver.FindElement(MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{text}\")"));
    }

    /// <summary>
    /// Найти элемент по частичному тексту
    /// </summary>
    protected AppiumElement FindByPartialText(string partialText)
    {
        return Driver.FindElement(MobileBy.AndroidUIAutomator($"new UiSelector().textContains(\"{partialText}\")"));
    }

    /// <summary>
    /// Найти элемент по XPath
    /// </summary>
    protected AppiumElement FindByXPath(string xpath)
    {
        return Driver.FindElement(By.XPath(xpath));
    }

    /// <summary>
    /// Найти элемент по классу (например android.widget.Button)
    /// </summary>
    protected AppiumElement FindByClass(string className)
    {
        return Driver.FindElement(By.ClassName(className));
    }

    /// <summary>
    /// Проверить существование элемента по AutomationId
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
    /// Проверить существование элемента по тексту
    /// </summary>
    protected bool ElementWithTextExists(string text, int timeoutSeconds = 5)
    {
        try
        {
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(timeoutSeconds);
            var element = FindByText(text);
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
    /// Скролл вниз на экране
    /// </summary>
    protected void ScrollDown()
    {
        var size = Driver.Manage().Window.Size;
        int startX = size.Width / 2;
        int startY = (int)(size.Height * 0.8);
        int endY = (int)(size.Height * 0.2);
        
        Driver.ExecuteScript("mobile: swipeGesture", new Dictionary<string, object>
        {
            { "left", startX - 50 },
            { "top", startY },
            { "width", 100 },
            { "height", startY - endY },
            { "direction", "up" },
            { "percent", 0.75 }
        });
    }

    /// <summary>
    /// Нажать кнопку "Назад" на устройстве
    /// </summary>
    protected void PressBack()
    {
        Driver.PressKeyCode(4); // KEYCODE_BACK
    }

    /// <summary>
    /// Сделать скриншот для отладки
    /// </summary>
    protected void TakeScreenshot(string name)
    {
        var screenshot = Driver.GetScreenshot();
        var path = Path.Combine(AppContext.BaseDirectory, "Screenshots");
        Directory.CreateDirectory(path);
        screenshot.SaveAsFile(Path.Combine(path, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png"));
    }

    /// <summary>
    /// Получить иерархию UI (для отладки)
    /// </summary>
    protected string GetPageSource()
    {
        return Driver.PageSource;
    }

    /// <summary>
    /// Сохранить иерархию UI в файл (для отладки)
    /// </summary>
    protected void SavePageSource(string name)
    {
        var source = GetPageSource();
        var path = Path.Combine(AppContext.BaseDirectory, "PageSources");
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.xml"), source);
    }

    public void Dispose()
    {
        Driver?.Quit();
        Driver?.Dispose();
        GC.SuppressFinalize(this);
    }
}
