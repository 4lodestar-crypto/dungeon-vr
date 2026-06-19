@echo off
set PROJ=C:\Projects\DungeonVR
set UNITY="C:\Program Files\Unity\Hub\Editor\6000.4.10f1-x86_64\Editor\Unity.exe"

REM Clean lock files
if exist "%PROJ%\Temp\UnityLockfile" del "%PROJ%\Temp\UnityLockfile"
if exist "%PROJ%\Library\ArtifactDB-lock" del "%PROJ%\Library\ArtifactDB-lock"
if exist "%PROJ%\Library\SourceAssetDB-lock" del "%PROJ%\Library\SourceAssetDB-lock"

REM Launch Unity NON-admin via start
start "" %UNITY% -projectPath "%PROJ%"

echo Unity launched in standard user mode.
