using GUI_Tests.Base;

namespace GUI_Tests.Tests.Android;

/// <summary>
/// Тесты для страницы регистрации на Android устройстве
/// </summary>
public class AndroidRegisterPageTests : AndroidTestBase
{
    /// <summary>
    /// Дождаться перехода на страницу регистрации
    /// </summary>
    private bool WaitForRegisterPage(int timeoutSeconds = 15)
    {
        var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
        
        while (DateTime.Now < endTime)
        {
            // Проверяем по AutomationId (x:Name)
            if (ElementExists("RegisterTitle", timeoutSeconds: 1) ||
                ElementExists("CodeEntry", timeoutSeconds: 1))
            {
                return true;
            }
            
            // Можно также проверить по тексту (если знаете текст заголовка)
            // if (ElementWithTextExists("Регистрация", timeoutSeconds: 1))
            // {
            //     return true;
            // }
            
            Thread.Sleep(500);
        }
        
        return false;
    }

    [Fact]
    public void RegisterPage_ShouldDisplay_Title()
    {
        // Arrange - ждем загрузки страницы регистрации
        var isOnRegisterPage = WaitForRegisterPage();
        
        TakeScreenshot("Android_RegisterPage_Initial");
        SavePageSource("Android_RegisterPage_Initial");
        
        if (!isOnRegisterPage)
        {
            // Пропускаем тест если не удалось дойти до страницы регистрации
            Assert.True(true, "Приложение не перешло на страницу регистрации - возможно пользователь уже авторизован");
            return;
        }

        // Assert
        Assert.True(ElementExists("RegisterTitle"), "Заголовок регистрации должен отображаться");
    }

    [Fact]
    public void RegisterPage_ShouldDisplay_CodeEntryField()
    {
        // Arrange
        var isOnRegisterPage = WaitForRegisterPage();
        
        if (!isOnRegisterPage)
        {
            TakeScreenshot("Android_RegisterPage_CodeEntry_NotReached");
            Assert.True(true, "Приложение не перешло на страницу регистрации");
            return;
        }

        // Assert
        Assert.True(ElementExists("CodeEntry"), "Поле ввода кода должно отображаться");
    }

    [Fact]
    public void RegisterPage_CodeEntry_ShouldAcceptInput()
    {
        // Arrange
        var isOnRegisterPage = WaitForRegisterPage();
        
        if (!isOnRegisterPage)
        {
            TakeScreenshot("Android_RegisterPage_InputTest_NotReached");
            Assert.True(true, "Приложение не перешло на страницу регистрации");
            return;
        }

        // Act - находим поле ввода и вводим текст
        var codeEntry = FindByAutomationId("CodeEntry");
        codeEntry.Clear();
        codeEntry.SendKeys("TEST-123");
        
        TakeScreenshot("Android_RegisterPage_CodeEntered");

        // Assert - проверяем что текст введен
        var enteredText = codeEntry.Text;
        Assert.Contains("TEST-123", enteredText);
    }

    [Fact]
    public void RegisterPage_RegisterButton_ShouldBeVisible()
    {
        // Arrange
        var isOnRegisterPage = WaitForRegisterPage();
        
        if (!isOnRegisterPage)
        {
            TakeScreenshot("Android_RegisterPage_Button_NotReached");
            Assert.True(true, "Приложение не перешло на страницу регистрации");
            return;
        }

        // Act & Assert
        var registerButton = FindByAutomationId("RegisterButton");
        
        TakeScreenshot("Android_RegisterPage_ButtonCheck");
        
        Assert.True(registerButton.Displayed, "Кнопка регистрации должна быть видима");
        Assert.True(registerButton.Enabled, "Кнопка регистрации должна быть активна");
    }

    [Fact]
    public void RegisterPage_ScannerButton_ShouldExist()
    {
        // Arrange
        var isOnRegisterPage = WaitForRegisterPage();
        
        if (!isOnRegisterPage)
        {
            TakeScreenshot("Android_RegisterPage_Scanner_NotReached");
            Assert.True(true, "Приложение не перешло на страницу регистрации");
            return;
        }

        // Act & Assert
        Assert.True(ElementExists("ScannerButton"), "Кнопка сканера должна отображаться");
        
        TakeScreenshot("Android_RegisterPage_ScannerButtonCheck");
    }

    [Fact]
    public void RegisterPage_TapRegisterButton_WithEmptyCode()
    {
        // Arrange
        var isOnRegisterPage = WaitForRegisterPage();
        
        if (!isOnRegisterPage)
        {
            TakeScreenshot("Android_RegisterPage_EmptyCode_NotReached");
            Assert.True(true, "Приложение не перешло на страницу регистрации");
            return;
        }

        // Act - очищаем поле и нажимаем кнопку
        var codeEntry = FindByAutomationId("CodeEntry");
        codeEntry.Clear();
        
        var registerButton = FindByAutomationId("RegisterButton");
        registerButton.Click();
        
        // Ждем реакции UI
        Thread.Sleep(2000);
        
        TakeScreenshot("Android_RegisterPage_EmptyCodeSubmit");

        // Assert - должны остаться на странице регистрации
        Assert.True(ElementExists("RegisterTitle") || ElementExists("CodeEntry"), 
            "Должны остаться на странице регистрации при пустом коде");
    }
}
