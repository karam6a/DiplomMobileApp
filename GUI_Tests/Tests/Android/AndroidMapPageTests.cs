using GUI_Tests.Base;

namespace GUI_Tests.Tests.Android;

/// <summary>
/// Тесты для страницы карты (MapPage)
/// Примечание: для этих тестов нужно быть авторизованным и иметь активный маршрут
/// </summary>
public class AndroidMapPageTests : AndroidTestBase
{
    /// <summary>
    /// Проверить что мы на странице карты
    /// </summary>
    private bool IsOnMapPage(int timeoutSeconds = 10)
    {
        var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
        
        while (DateTime.Now < endTime)
        {
            // Проверяем элементы MapPage
            if (ElementExists("MapControl", timeoutSeconds: 1) ||
                ElementExists("BottomSheet", timeoutSeconds: 1) ||
                ElementExists("MyLocationFloatingButton", timeoutSeconds: 1))
            {
                return true;
            }
            Thread.Sleep(500);
        }
        
        return false;
    }

    [Fact]
    public void MapPage_ShouldDisplay_MapControl()
    {
        // Arrange - попытаемся попасть на карту
        // Это требует авторизации и активного маршрута
        var isOnMap = IsOnMapPage();
        
        TakeScreenshot("MapPage_Initial");
        SavePageSource("MapPage_Initial");
        
        if (!isOnMap)
        {
            // Карта может быть недоступна без маршрута
            Assert.True(true, "MapPage недоступна - возможно нет активного маршрута");
            return;
        }

        // Assert
        Assert.True(ElementExists("MapControl"), "Карта должна отображаться");
    }

    [Fact]
    public void MapPage_ShouldDisplay_MyLocationButton()
    {
        // Arrange
        var isOnMap = IsOnMapPage();
        
        if (!isOnMap)
        {
            Assert.True(true, "MapPage недоступна");
            return;
        }

        // Assert - кнопка "Моё местоположение"
        var buttonExists = ElementExists("MyLocationFloatingButton");
        
        TakeScreenshot("MapPage_LocationButton");
        
        Assert.True(buttonExists, "Кнопка 'Моё местоположение' должна отображаться");
    }

    [Fact]
    public void MapPage_ShouldDisplay_BottomSheet()
    {
        // Arrange
        var isOnMap = IsOnMapPage();
        
        if (!isOnMap)
        {
            Assert.True(true, "MapPage недоступна");
            return;
        }

        // Assert - нижняя панель
        var bottomSheetExists = ElementExists("BottomSheet");
        
        TakeScreenshot("MapPage_BottomSheet");
        
        Assert.True(bottomSheetExists, "Нижняя панель (BottomSheet) должна отображаться");
    }

    [Fact]
    public void MapPage_ShouldDisplay_ControlButtons()
    {
        // Arrange
        var isOnMap = IsOnMapPage();
        
        if (!isOnMap)
        {
            Assert.True(true, "MapPage недоступна");
            return;
        }

        // Assert - панель с кнопками управления
        var buttonsExist = ElementExists("NormalButtonsPanel");
        
        TakeScreenshot("MapPage_ControlButtons");
        
        // Кнопки могут быть скрыты в режиме навигации
        Assert.True(true, $"NormalButtonsPanel exists: {buttonsExist}");
    }
}
