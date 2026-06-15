@echo off
REM Applies the AddImportExportToCanvas tool to SampleScene.unity via Unity batch mode.
REM IMPORTANT: Close the Unity Editor for this project before running this script.
setlocal

set "PROJECT_PATH=%~dp0.."
set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe"
set "LOG_FILE=%PROJECT_PATH%\Logs\ApplyImportExport.log"

if not exist "%UNITY_EXE%" (
  echo Unity not found at: %UNITY_EXE%
  echo Edit this script and set UNITY_EXE to your installed Unity 2022.3 path.
  exit /b 1
)

echo Running Unity in batch mode...
echo Log: %LOG_FILE%

"%UNITY_EXE%" -batchmode -nographics -quit ^
  -projectPath "%PROJECT_PATH%" ^
  -executeMethod LevelMaker.Editor.RunAddImportExport.Run ^
  -logFile "%LOG_FILE%"

if %ERRORLEVEL% NEQ 0 (
  echo.
  echo Unity exited with code %ERRORLEVEL%. Check log:
  type "%LOG_FILE%"
  exit /b %ERRORLEVEL%
)

echo.
echo Done. SampleScene.unity now has SAVE / LOAD buttons and LevelListPanel.
echo Reopen the project in Unity to see the changes.
exit /b 0
