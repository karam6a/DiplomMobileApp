@echo off
echo ============================================
echo    Запуск Android GUI тестов
echo ============================================
echo.

REM Проверяем запущен ли Appium
netstat -an | find "4723" > nul
if errorlevel 1 (
    echo [ОШИБКА] Appium не запущен!
    echo.
    echo Откройте новое окно командной строки и выполните:
    echo    appium
    echo.
    echo Затем запустите этот скрипт снова.
    pause
    exit /b 1
)

echo [OK] Appium запущен
echo.
echo Запуск тестов...
echo.

REM Запуск тестов с генерацией XML отчета
dotnet test --filter "FullyQualifiedName~Android" --logger "xunit;LogFilePath=TestResults\results.xml" --results-directory TestResults

echo.
echo ============================================
echo    Тесты завершены!
echo ============================================
echo.
echo Результаты сохранены в: TestResults\results.xml
echo Скриншоты в: bin\Debug\net9.0\Screenshots\
echo.
pause
