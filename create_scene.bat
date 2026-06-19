@echo off
REM Kill any stale Unity processes
taskkill /f /im Unity.exe 2>nul
taskkill /f /im UnityHub.exe 2>nul
timeout /t 3 /nobreak >nul

REM Remove lock files
if exist "C:\Projects\DungeonVR\Temp\UnityLockfile" del "C:\Projects\DungeonVR\Temp\UnityLockfile"
if exist "C:\Projects\DungeonVR\Library\ArtifactDB-lock" del "C:\Projects\DungeonVR\Library\ArtifactDB-lock"
if exist "C:\Projects\DungeonVR\Library\SourceAssetDB-lock" del "C:\Projects\DungeonVR\Library\SourceAssetDB-lock"

REM Logs dir
if not exist "C:\Projects\DungeonVR\Logs" mkdir "C:\Projects\DungeonVR\Logs"

REM Launch Unity in batchmode to create the test scene
echo Launching Unity in batchmode...
"C:\Program Files\Unity\Hub\Editor\6000.4.10f1-x86_64\Editor\Unity.exe" -projectPath "C:\Projects\DungeonVR" -executeMethod CreateProceduralTestScene.Create -quit -batchmode -logFile "C:\Projects\DungeonVR\Logs\create-scene.log"
echo Unity exited with code: %ERRORLEVEL%
