@echo off

echo Build and install WinWidgetTimer

:: Kill widget if running, so it can be re-built
taskkill /IM WinWidgetTimer.exe /F 2>nul
dotnet publish WinWidgetTimer.csproj -c Release --self-contained false

:: Make directory to hold the dll's and assets
mkdir c:\opt\bin\winwidgets 2>nul
xcopy /E /Y bin\Release\net10.0-windows\publish\* c:\opt\bin\winwidgets\

echo start "" c:\opt\bin\winwidgets\WinWidgetTimer.exe > c:\opt\bin\WinWidgetTimer.bat
