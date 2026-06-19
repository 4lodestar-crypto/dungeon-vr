@echo off
setlocal

set PROJ=C:\Projects\DungeonVR
set UNITY="C:\Program Files\Unity\Hub\Editor\6000.4.10f1-x86_64\Editor\Unity.exe"

REM Clean up lock files
echo Cleaning lock files...
if exist "%PROJ%\Temp\UnityLockfile" del "%PROJ%\Temp\UnityLockfile"
if exist "%PROJ%\Library\ArtifactDB-lock" del "%PROJ%\Library\ArtifactDB-lock"
if exist "%PROJ%\Library\SourceAssetDB-lock" del "%PROJ%\Library\SourceAssetDB-lock"

REM Create Logs dir
if not exist "%PROJ%\Logs" mkdir "%PROJ%\Logs"

REM Wait a moment for processes to settle
timeout /t 2 /nobreak >nul

REM Launch Unity in batchmode
echo Creating test scene...
%UNITY% -projectPath "%PROJ%" -executeMethod CreateProceduralTestScene.Create -quit -batchmode -logFile "%PROJ%\Logs\CreateScene.log"

echo Unity exit code: %ERRORLEVEL%

REM Check result
if %ERRORLEVEL%==0 (
    echo SUCCESS: Scene created!
) else (
    echo FAILED: Check the log for details.
)
