# Robocopy Network Deployment Guide

Complete guide for deploying Unity builds to multiple test PCs over a network using Robocopy.

## Table of Contents
- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Robocopy Basics](#robocopy-basics)
- [Deployment Scenarios](#deployment-scenarios)
- [Automated Deployment Scripts](#automated-deployment-scripts)
- [Advanced Features](#advanced-features)
- [Troubleshooting](#troubleshooting)

---

## Overview

**Robocopy** (Robust File Copy) is a powerful Windows command-line tool perfect for deploying Unity builds to multiple test machines over a network.

**Why use Robocopy?**
- ✅ **Fast**: Only copies changed files
- ✅ **Reliable**: Handles network interruptions
- ✅ **Automated**: Script once, deploy many times
- ✅ **Verified**: Can verify copies with checksums
- ✅ **Logged**: Detailed logs for troubleshooting

**Common Use Cases:**
- Deploy builds to QA/testing machines
- Update game clients across LAN
- Distribute to multiplayer test rigs
- Backup builds to network storage

---

## Prerequisites

### 1. Network Setup

**All test PCs must:**
- Be on the same local network
- Have network sharing enabled
- Have accessible shared folders

**Server/Source PC:**
- Has the Unity build files
- Can access test PCs over network

### 2. Shared Folders on Test PCs

Each test PC needs a shared folder where builds will be deployed.

**Setting up shared folder (Windows 10/11):**

1. Create folder: `C:\GameBuilds`
2. Right-click folder > Properties > Sharing tab
3. Click "Advanced Sharing"
4. Check "Share this folder"
5. Click "Permissions" > Add "Everyone" > Allow Full Control
6. Note the network path: `\\PC-NAME\GameBuilds`

**Quick test:**
```cmd
:: From your development PC, test access:
dir \\TEST-PC-01\GameBuilds
```

If you see the directory contents, sharing works!

### 3. Network Credentials

**Option A: Same User Account**
- Use same username/password on all PCs
- Windows will authenticate automatically

**Option B: Saved Credentials**
```cmd
:: Save credentials for target PC
cmdkey /add:TEST-PC-01 /user:TestUser /pass:Password123
```

**Option C: Script with credentials (less secure)**
- Use `NET USE` command in script
- See examples below

---

## Quick Start

### Basic Deployment

**1. Build your Unity project**
- File > Build Settings > Build
- Build to: `C:\UnityBuilds\MyGame`

**2. Test manual copy to one PC**
```cmd
robocopy "C:\UnityBuilds\MyGame" "\\TEST-PC-01\GameBuilds\MyGame" /MIR
```

**3. Verify**
- Go to test PC
- Check `C:\GameBuilds\MyGame`
- Run the game

**4. Deploy to all test PCs**
```cmd
robocopy "C:\UnityBuilds\MyGame" "\\TEST-PC-01\GameBuilds\MyGame" /MIR
robocopy "C:\UnityBuilds\MyGame" "\\TEST-PC-02\GameBuilds\MyGame" /MIR
robocopy "C:\UnityBuilds\MyGame" "\\TEST-PC-03\GameBuilds\MyGame" /MIR
```

Done! Your build is now on all test machines.

---

## Robocopy Basics

### Command Structure

```cmd
robocopy [source] [destination] [options]
```

### Essential Options

| Option | Description | Example |
|--------|-------------|---------|
| `/MIR` | Mirror (sync) directories, delete extra files | `/MIR` |
| `/E` | Copy subdirectories including empty ones | `/E` |
| `/COPYALL` | Copy all file info (timestamps, attributes) | `/COPYALL` |
| `/R:n` | Retry n times on failed copies (default 1 million!) | `/R:3` |
| `/W:n` | Wait n seconds between retries | `/W:5` |
| `/LOG:file` | Write log to file | `/LOG:deploy.log` |
| `/NP` | No progress (cleaner output) | `/NP` |
| `/XF file` | Exclude files matching pattern | `/XF *.pdb` |
| `/XD dir` | Exclude directories | `/XD Logs` |

### Common Combinations

**Fast sync (recommended for Unity builds):**
```cmd
robocopy [source] [dest] /MIR /R:3 /W:5 /NP
```

**Full sync with logging:**
```cmd
robocopy [source] [dest] /MIR /COPYALL /R:3 /W:5 /LOG:deploy.log
```

**Sync without deleting extra files:**
```cmd
robocopy [source] [dest] /E /R:3 /W:5
```

---

## Deployment Scenarios

### Scenario 1: Single Build to Multiple Test PCs

**Setup:**
- 1 Development PC
- 4 Test PCs waiting for builds
- All on same LAN

**Batch Script: `deploy_to_all.bat`**
```batch
@echo off
echo ========================================
echo Unity Build Deployment Script
echo ========================================
echo.

set SOURCE=C:\UnityBuilds\MyGame
set DEST_FOLDER=GameBuilds\MyGame

echo Source: %SOURCE%
echo.
echo Deploying to test machines...
echo.

:: Test PC 1
echo [1/4] Deploying to TEST-PC-01...
robocopy "%SOURCE%" "\\TEST-PC-01\%DEST_FOLDER%" /MIR /R:3 /W:5 /NP /NDL /NFL
echo.

:: Test PC 2
echo [2/4] Deploying to TEST-PC-02...
robocopy "%SOURCE%" "\\TEST-PC-02\%DEST_FOLDER%" /MIR /R:3 /W:5 /NP /NDL /NFL
echo.

:: Test PC 3
echo [3/4] Deploying to TEST-PC-03...
robocopy "%SOURCE%" "\\TEST-PC-03\%DEST_FOLDER%" /MIR /R:3 /W:5 /NP /NDL /NFL
echo.

:: Test PC 4
echo [4/4] Deploying to TEST-PC-04...
robocopy "%SOURCE%" "\\TEST-PC-04\%DEST_FOLDER%" /MIR /R:3 /W:5 /NP /NDL /NFL
echo.

echo ========================================
echo Deployment complete!
echo ========================================
pause
```

**Usage:**
1. Save as `deploy_to_all.bat`
2. Edit `SOURCE` path to your build location
3. Edit PC names to match your network
4. Double-click to run

---

### Scenario 2: Incremental Updates

Only copy files that changed since last deployment.

**Batch Script: `deploy_update.bat`**
```batch
@echo off
set SOURCE=C:\UnityBuilds\MyGame
set DEST=\\TEST-PC-01\GameBuilds\MyGame
set LOG=deploy_%date:~-4,4%%date:~-7,2%%date:~-10,2%.log

echo Deploying incremental update...
echo Log: %LOG%

robocopy "%SOURCE%" "%DEST%" /E /R:3 /W:5 /LOG:"%LOG%" /XO

echo.
echo Update complete! Check %LOG% for details.
pause
```

**Options explained:**
- `/E` - Copy all subdirectories (including empty)
- `/XO` - Exclude older files (only copy newer)
- `/LOG` - Create dated log file

---

### Scenario 3: Unity Editor Build + Auto-Deploy

Integrate with Unity build process.

**Unity Build Script: `Assets/Editor/AutoDeploy.cs`**
```csharp
using UnityEditor;
using UnityEngine;
using System.Diagnostics;

public class AutoDeploy
{
    [MenuItem("Build/Build and Deploy to Test PCs")]
    static void BuildAndDeploy()
    {
        // Build settings
        string buildPath = "C:/UnityBuilds/MyGame/MyGame.exe";
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/MainScene.unity" },
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        // Build the project
        UnityEngine.Debug.Log("Building project...");
        BuildPipeline.Build(buildOptions);

        // Deploy to test PCs
        UnityEngine.Debug.Log("Deploying to test PCs...");

        Process process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = "/c deploy_to_all.bat";
        process.StartInfo.WorkingDirectory = "C:/UnityBuilds/Scripts";
        process.Start();
        process.WaitForExit();

        UnityEngine.Debug.Log("Build and deployment complete!");
    }
}
```

**Usage:**
1. Add script to `Assets/Editor/`
2. In Unity: `Build > Build and Deploy to Test PCs`
3. Automatically builds and deploys!

---

### Scenario 4: Parallel Deployment

Deploy to multiple PCs simultaneously for faster distribution.

**PowerShell Script: `deploy_parallel.ps1`**
```powershell
# Configuration
$source = "C:\UnityBuilds\MyGame"
$destFolder = "GameBuilds\MyGame"
$testPCs = @("TEST-PC-01", "TEST-PC-02", "TEST-PC-03", "TEST-PC-04")

# Function to deploy to single PC
function Deploy-ToPC {
    param($pcName)

    $dest = "\\$pcName\$destFolder"
    Write-Host "Deploying to $pcName..." -ForegroundColor Cyan

    robocopy $source $dest /MIR /R:3 /W:5 /NP /NDL /NFL

    if ($LASTEXITCODE -lt 8) {
        Write-Host "✓ $pcName complete" -ForegroundColor Green
    } else {
        Write-Host "✗ $pcName failed" -ForegroundColor Red
    }
}

# Deploy in parallel
Write-Host "Starting parallel deployment..." -ForegroundColor Yellow
Write-Host ""

$jobs = $testPCs | ForEach-Object {
    Start-Job -ScriptBlock ${function:Deploy-ToPC} -ArgumentList $_
}

# Wait for all jobs
$jobs | Wait-Job | Receive-Job

Write-Host ""
Write-Host "All deployments complete!" -ForegroundColor Green
```

**Usage:**
```powershell
powershell -ExecutionPolicy Bypass -File deploy_parallel.ps1
```

**Benefits:**
- 4x faster for 4 PCs (deploys simultaneously)
- Progress shown for each PC
- Color-coded success/failure

---

## Automated Deployment Scripts

### Complete Production Script

**`deploy_production.bat`**
```batch
@echo off
setlocal enabledelayedexpansion

:: ========================================
:: Configuration
:: ========================================
set BUILD_NAME=MyGame
set BUILD_VERSION=1.0.0
set SOURCE=C:\UnityBuilds\%BUILD_NAME%
set DEST_FOLDER=GameBuilds\%BUILD_NAME%
set LOG_DIR=C:\DeploymentLogs
set TIMESTAMP=%date:~-4,4%%date:~-7,2%%date:~-10,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set TIMESTAMP=%TIMESTAMP: =0%

:: Test PC list
set TEST_PCS=TEST-PC-01 TEST-PC-02 TEST-PC-03 TEST-PC-04

:: ========================================
:: Pre-deployment checks
:: ========================================
echo ========================================
echo Unity Build Deployment v%BUILD_VERSION%
echo ========================================
echo.

:: Check if source exists
if not exist "%SOURCE%" (
    echo ERROR: Source folder not found: %SOURCE%
    pause
    exit /b 1
)

:: Create log directory
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

echo Source: %SOURCE%
echo Timestamp: %TIMESTAMP%
echo.

:: ========================================
:: Deploy to each PC
:: ========================================
set /a COUNTER=0
set /a TOTAL=0
set /a SUCCESS=0
set /a FAILED=0

:: Count total PCs
for %%P in (%TEST_PCS%) do set /a TOTAL+=1

echo Starting deployment to %TOTAL% test PCs...
echo.

for %%P in (%TEST_PCS%) do (
    set /a COUNTER+=1
    echo [!COUNTER!/%TOTAL%] Deploying to %%P...

    :: Test connection first
    ping %%P -n 1 -w 1000 >nul
    if errorlevel 1 (
        echo   ERROR: Cannot reach %%P
        set /a FAILED+=1
    ) else (
        :: Deploy
        set DEST=\\%%P\%DEST_FOLDER%
        set LOG=%LOG_DIR%\%%P_%TIMESTAMP%.log

        robocopy "%SOURCE%" "!DEST!" /MIR /R:3 /W:5 /LOG:"!LOG!" /NP /NDL /NFL

        :: Check robocopy exit code
        if !errorlevel! lss 8 (
            echo   SUCCESS: Deployed to %%P
            set /a SUCCESS+=1
        ) else (
            echo   FAILED: Deployment to %%P failed (code !errorlevel!)
            set /a FAILED+=1
        )
    )
    echo.
)

:: ========================================
:: Summary
:: ========================================
echo ========================================
echo Deployment Summary
echo ========================================
echo Total PCs:    %TOTAL%
echo Successful:   %SUCCESS%
echo Failed:       %FAILED%
echo Logs:         %LOG_DIR%
echo ========================================
echo.

if %FAILED% gtr 0 (
    echo WARNING: Some deployments failed. Check logs.
) else (
    echo All deployments successful!
)

pause
```

**Features:**
- Pre-deployment connectivity checks
- Detailed logging per PC
- Success/failure tracking
- Timestamped logs
- Summary report

---

### Network Credential Management

**If PCs require authentication:**

**Script: `deploy_with_auth.bat`**
```batch
@echo off

:: Map network drives with credentials
echo Connecting to test PCs...

net use \\TEST-PC-01\IPC$ /user:DOMAIN\TestUser Password123
net use \\TEST-PC-02\IPC$ /user:DOMAIN\TestUser Password123
net use \\TEST-PC-03\IPC$ /user:DOMAIN\TestUser Password123

:: Deploy
call deploy_to_all.bat

:: Disconnect
echo Disconnecting...
net use \\TEST-PC-01\IPC$ /delete
net use \\TEST-PC-02\IPC$ /delete
net use \\TEST-PC-03\IPC$ /delete

echo Done!
pause
```

**More secure option:**
Store credentials once:
```cmd
cmdkey /add:TEST-PC-01 /user:TestUser /pass:Password123
```

Then regular deployment scripts work without credentials in code.

---

## Advanced Features

### 1. Exclude Files

Don't deploy Unity debug symbols or logs:

```batch
robocopy "%SOURCE%" "%DEST%" /MIR /XF *.pdb /XD Logs Temp
```

### 2. Bandwidth Throttling

Limit network usage during business hours:

```batch
robocopy "%SOURCE%" "%DEST%" /MIR /IPG:100
```
(`/IPG:100` = 100ms delay between packets)

### 3. Verify Copies

Use checksum verification:

```batch
robocopy "%SOURCE%" "%DEST%" /MIR /FFT /Z
```
- `/FFT` - Assume FAT file times (2-second granularity)
- `/Z` - Restartable mode (survives network interruptions)

### 4. Monitor Mode

Continuously watch for changes and deploy:

```batch
robocopy "%SOURCE%" "%DEST%" /MIR /MON:1 /MOT:5
```
- `/MON:1` - Monitor for 1 change
- `/MOT:5` - Monitor for changes every 5 minutes

### 5. Email Notifications

**PowerShell with email:**
```powershell
# Deploy
robocopy "C:\UnityBuilds\MyGame" "\\TEST-PC-01\GameBuilds\MyGame" /MIR

# Send email
Send-MailMessage -From "build@company.com" -To "qa@company.com" `
    -Subject "Build Deployed" -Body "MyGame deployed to test PCs" `
    -SmtpServer "smtp.company.com"
```

---

## Troubleshooting

### Error: "Access is denied"

**Causes:**
- Insufficient permissions
- File in use
- Network credentials missing

**Solutions:**
```batch
:: Run as administrator
:: Right-click .bat > Run as administrator

:: Or add credentials
net use \\TEST-PC-01\IPC$ /user:USERNAME PASSWORD
```

### Error: "Network path not found"

**Causes:**
- PC offline/unreachable
- Network sharing disabled
- Firewall blocking

**Solutions:**
```batch
:: Test connectivity
ping TEST-PC-01

:: Test share access
dir \\TEST-PC-01\GameBuilds

:: Check firewall (allow File and Printer Sharing)
```

### Files Not Copying

**Check robocopy exit codes:**

| Code | Meaning |
|------|---------|
| 0 | No files copied (everything already in sync) |
| 1 | Files copied successfully |
| 2 | Extra files/directories detected |
| 4 | Mismatched files/directories |
| 8+ | Errors occurred |

**View detailed log:**
```batch
robocopy [source] [dest] /MIR /V /LOG:detailed.log
notepad detailed.log
```

### Slow Deployment

**Optimizations:**
```batch
:: Disable unnecessary features
robocopy [source] [dest] /MIR /J /R:1 /W:1

:: /J = Unbuffered I/O (faster for large files)
:: /R:1 = Only 1 retry
:: /W:1 = Wait 1 second between retries
```

### Robocopy Won't Stop Retrying

**Default robocopy retries 1 MILLION times!**

Always set retry limit:
```batch
robocopy [source] [dest] /MIR /R:3 /W:5
```

---

## Testing Workflow

### Recommended Testing Process

1. **Build in Unity**
   ```
   File > Build Settings > Build
   Output: C:\UnityBuilds\MyGame\
   ```

2. **Test locally**
   - Run the .exe locally first
   - Verify game works

3. **Deploy to one test PC**
   ```batch
   robocopy "C:\UnityBuilds\MyGame" "\\TEST-PC-01\GameBuilds\MyGame" /MIR /R:3 /W:5
   ```

4. **Verify on test PC**
   - Go to test PC
   - Run game
   - Test functionality

5. **Deploy to all test PCs**
   ```batch
   deploy_to_all.bat
   ```

6. **Network testing**
   - Run server on one PC
   - Connect clients from others
   - Test multiplayer features

---

## Integration with Unity Networking

### Complete Workflow

**1. Build Unity project with networking**
```
Unity Editor > Build Settings > Build
Include networking components (SimpleServer, SimpleNetworkMessenger)
```

**2. Deploy to test PCs**
```batch
deploy_to_all.bat
```

**3. Start server on one PC**
```
On TEST-PC-01: Run MyGame.exe
In-game: Click "Start Server"
Note IP address: 192.168.1.100
```

**4. Connect clients from other PCs**
```
On TEST-PC-02, 03, 04: Run MyGame.exe
In-game: Enter server IP: 192.168.1.100
Click "Connect"
```

**5. Test messaging**
- Send chat messages
- Test position sync
- Verify all clients receive updates

---

## Quick Reference

### Basic Commands

```batch
:: Full mirror sync
robocopy [source] [dest] /MIR /R:3 /W:5

:: Copy only, don't delete
robocopy [source] [dest] /E /R:3 /W:5

:: With logging
robocopy [source] [dest] /MIR /LOG:deploy.log /R:3 /W:5

:: Quiet mode (minimal output)
robocopy [source] [dest] /MIR /NP /NDL /NFL /NJH /R:3 /W:5
```

### PC List Template

```batch
set TEST_PCS=TEST-PC-01 TEST-PC-02 TEST-PC-03 TEST-PC-04

for %%P in (%TEST_PCS%) do (
    robocopy "%SOURCE%" "\\%%P\%DEST_FOLDER%" /MIR /R:3 /W:5
)
```

---

## Additional Resources

- **Robocopy Documentation**: `robocopy /?` in cmd
- **Unity Build Documentation**: https://docs.unity3d.com/Manual/BuildSettings.html
- **Windows Networking**: https://support.microsoft.com/en-us/windows

---

Happy deploying! 🚀
