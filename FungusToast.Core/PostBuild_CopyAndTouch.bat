@echo on
setlocal

:: Get the directory of the script (which is also the project folder)
set SCRIPT_DIR=%~dp0

:: Define paths relative to FungusToast.Core\
set SOURCE=%SCRIPT_DIR%bin\Debug\netstandard2.1
set TARGET=%SCRIPT_DIR%..\Fungus Toast\Assets\Plugins
set TOUCH_FILE=%SCRIPT_DIR%..\Fungus Toast\Assets\Scripts\Unity\ForceRecompile.cs

echo Copying FungusToast.Core artifacts to Unity...

if not exist "%TARGET%" (
    echo Creating target folder...
    mkdir "%TARGET%"
)

xcopy /Y /I "%SOURCE%\FungusToast.Core.dll" "%TARGET%" || goto :error
xcopy /Y /I "%SOURCE%\FungusToast.Core.pdb" "%TARGET%" || goto :error
if exist "%SOURCE%\FungusToast.Core.xml" xcopy /Y /I "%SOURCE%\FungusToast.Core.xml" "%TARGET%"

echo Touching ForceRecompile.cs...
powershell -ExecutionPolicy Bypass -Command "$timestamp = '// Touched on: ' + (Get-Date -Format o); Set-Content -Path '%TOUCH_FILE%' -Value $timestamp" || goto :error

echo Done.
exit /b 0

:error
echo ERROR OCCURRED: %errorlevel%
exit /b 1
