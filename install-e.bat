@echo off

echo Build and install WinWidgetTimer

@rem TODO - get bin directory from a global environment. 
set dstDir=e:\opt\bin


:: Kill widget if running, so it can be re-built
taskkill /IM WinWidgetTimer.exe /F 2>nul
dotnet publish WinWidgetTimer.csproj -c Release --self-contained false

:: Make directory to hold the dll's and assets
mkdir %dstDir%\winwidgets 2>nul
xcopy /E /Y bin\Release\net10.0-windows\publish\* %dstDir%\winwidgets\

echo start "" %dstDir%\winwidgets\WinWidgetTimer.exe > %dstDir%\WinWidgetTimer.bat
