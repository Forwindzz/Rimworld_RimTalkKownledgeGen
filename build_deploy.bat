@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul

REM =========================
REM GenKnowledge Build + Deploy
REM =========================
set "ROOT_DIR=%~dp0"
set "RIMWORLD_DIR=E:\Program Files\Steam\steamapps\common\RimWorld"
set "MOD_NAME=RimTalk_GenKnowledge"
set "MOD_DEPLOY_DIR=%RIMWORLD_DIR%\Mods\%MOD_NAME%"
set "FALLBACK_DEPLOY_DIR=%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Mods\%MOD_NAME%"
set "BUILD_DIR=%ROOT_DIR%Build"

echo [INFO] Root: %ROOT_DIR%
echo [INFO] RimWorld: %RIMWORLD_DIR%
echo [INFO] Deploy: %MOD_DEPLOY_DIR%

if not exist "%RIMWORLD_DIR%\RimWorldWin64.exe" (
    echo [ERROR] RimWorld path invalid: %RIMWORLD_DIR%
    exit /b 1
)

if not exist "%ROOT_DIR%.vscode\mod.csproj" (
    echo [ERROR] Project file not found: %ROOT_DIR%.vscode\mod.csproj
    exit /b 1
)

if exist "%BUILD_DIR%" (
    rmdir /s /q "%BUILD_DIR%"
)
mkdir "%BUILD_DIR%" || (
    echo [ERROR] Failed to create Build directory.
    exit /b 1
)

pushd "%ROOT_DIR%"
echo [INFO] Building...
dotnet build ".\code.sln" -c Release -p:Restore=false -p:BaseIntermediateOutputPath=..\Build\obj\ -p:MSBuildProjectExtensionsPath=..\Build\obj\
if errorlevel 1 (
    echo [ERROR] Build failed.
    popd
    exit /b 1
)

if not exist ".\Build\1.6\Assemblies\RimTalk_GenKnowledge.dll" (
    echo [ERROR] Build output missing: .\Build\1.6\Assemblies\RimTalk_GenKnowledge.dll
    popd
    exit /b 1
)

echo [INFO] Deploying to mod folder...
call :deploy_to "%MOD_DEPLOY_DIR%"
if errorlevel 1 (
    echo [WARN] Primary deploy failed, trying fallback user Mods directory...
    call :deploy_to "%FALLBACK_DEPLOY_DIR%"
    if errorlevel 1 (
        echo [ERROR] Deploy failed on both primary and fallback paths.
        popd
        exit /b 1
    )
)

echo [OK] Build and deploy completed.
popd
exit /b 0

:deploy_to
set "TARGET=%~1"
echo [INFO] Target: %TARGET%
if not exist "%TARGET%" mkdir "%TARGET%" >nul 2>nul
if not exist "%TARGET%\1.6" mkdir "%TARGET%\1.6" >nul 2>nul
if not exist "%TARGET%\1.6\Assemblies" mkdir "%TARGET%\1.6\Assemblies" >nul 2>nul

robocopy ".\About" "%TARGET%\About" /E /NFL /NDL /NJH /NJS /NP >nul
if errorlevel 8 goto :deploy_fail

robocopy ".\1.6" "%TARGET%\1.6" /E /XD Assemblies /NFL /NDL /NJH /NJS /NP >nul
if errorlevel 8 goto :deploy_fail

robocopy ".\Build\1.6\Assemblies" "%TARGET%\1.6\Assemblies" /E /NFL /NDL /NJH /NJS /NP >nul
if errorlevel 8 goto :deploy_fail

echo [OK] Deployed to: %TARGET%
echo [OK] DLL: %TARGET%\1.6\Assemblies\RimTalk_GenKnowledge.dll
exit /b 0

:deploy_fail
echo [WARN] Deploy failed for target: %TARGET%
exit /b 1
