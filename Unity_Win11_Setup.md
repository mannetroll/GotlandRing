# Build Gotland Ring on Windows 11

This guide sets up a Windows 11 x64 computer to edit the game, build `Build/Windows/GotlandRing.exe`, and package `GotlandRing-Portable.exe`. Run the commands from the repository root in **PowerShell 7** unless stated otherwise. For the Mac build, see [Unity_macOS_Setup.md](Unity_macOS_Setup.md).

The project pins **Unity 6000.3.25f1 LTS**, revision **e1dba0a9aba4**, in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt). The game uses **Mono**, the **built-in renderer**, **Direct3D 11**, and **Windows x64**. The separate portable launcher targets **.NET 10 / win-x64**.

## 1. Tools and binaries to install

| Component | Required for | Download / selection |
|---|---|---|
| Windows 11 x64 and a Direct3D 11 capable GPU | Editor and game | Use current graphics drivers from your PC/GPU manufacturer. This guide targets Intel/AMD x64 PCs. |
| Git for Windows, x64 | Cloning and updating the source | [Git for Windows installer](https://git-scm.com/install/windows). Make Git available from the command line. |
| Unity Hub | Installing the editor and managing its license | [Unity Hub installation and downloads](https://docs.unity.com/en-us/hub/install-hub). Select Windows. |
| Unity Editor **6000.3.25f1**, Windows x64 | Compiling the game | [Exact editor release and installers](https://unity.com/releases/editor/whats-new/6000.3.25f1). Choose **Windows**, not Windows ARM64. |
| Windows standalone **Mono** player support | Building the game | Included with the Windows editor. The separate **Windows Build Support (IL2CPP)** module is not used by this project. |
| PowerShell **7**, stable x64 | Running `scripts/Package-Game.ps1` and the commands below | [Microsoft's PowerShell installation guide](https://learn.microsoft.com/en-us/powershell/scripting/install/install-powershell-on-windows). Launch `pwsh`, not Windows PowerShell 5.1. |
| .NET **10 SDK**, Windows x64 | Building the portable launcher | [.NET 10 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0): choose **SDK → Windows → x64 installer**. A runtime-only download cannot compile the launcher. |
| A code editor, optional | Editing C# and project files | [Visual Studio Code](https://code.visualstudio.com/download), Windows x64. Unity can build without an external editor. |
| GitHub CLI, optional | Uploading release assets | [GitHub CLI](https://cli.github.com/), Windows installer. Requires repository write access when publishing. |

Use a local SSD checkout and allow room for the editor, imported assets in `Library/`, and build outputs. Unity recommends at least 8 GB RAM; 16 GB or more is a practical choice for development. See [Unity 6.3 system requirements](https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html).

The current Mono build does not need Visual Studio's C++ workload, the IL2CPP module, or a separately installed Windows SDK. Those native compiler requirements apply if the project is changed to IL2CPP; see [Unity's Windows requirements](https://docs.unity3d.com/6000.3/Documentation/Manual/windows-requirements-and-compatibility.html). Unity supplies its own C# compiler and Mono runtime. No separate Mono, Java, Android SDK/NDK, Python, Node.js, Blender, or 7-Zip installation is needed for this build.

### Install command-line tools

These commands can be run in the Windows terminal before PowerShell 7 is installed. Alternatively, use the installers linked above.

```powershell
winget install --id Git.Git --exact --source winget
winget install --id Microsoft.PowerShell --exact --source winget
winget install --id Microsoft.DotNet.SDK.10 --exact --source winget
```

The package IDs are documented by [Git](https://git-scm.com/install/windows), [PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/install/install-powershell-on-windows), and [.NET](https://learn.microsoft.com/en-us/dotnet/core/install/windows). Open a new **PowerShell 7** terminal after installation so its PATH is refreshed:

```powershell
$PSVersionTable.PSVersion
git --version
dotnet --list-sdks
```

Expect PowerShell major version `7` and an installed `10.0.x` SDK. The repository does not pin a .NET SDK patch version; use a supported stable .NET 10 SDK.

## 2. Install and activate Unity

1. Install Unity Hub and sign in with your Unity account.
2. Activate a valid Unity license in Hub's license settings. Use Unity Personal if eligible, or your assigned paid license. Finish activation before attempting a batch build.
3. Open the [6000.3.25f1 release page](https://unity.com/releases/editor/whats-new/6000.3.25f1) and install this exact editor through Hub. If it is absent from Hub's default list, use the release/archive installation option.
4. Keep Windows Mono support. Android, iOS, Web, dedicated-server and IL2CPP modules are unnecessary for the Windows build described here.

The standard editor executable is:

```text
C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe
```

If you install the editor manually, locate it in Hub and use its actual path below. Module installers must match the editor version. See [Unity's editor installation guide](https://docs.unity.com/en-us/hub/install-editors).

## 3. Get the complete project

Choose a parent folder for the checkout, then run:

```powershell
git clone https://github.com/mannetroll/GotlandRing.git
Set-Location GotlandRing
Get-Content ProjectSettings/ProjectVersion.txt
```

The repository includes the required C# code, scene, shaders, textures, sky, icon, engine recording, track CSVs, licenses, and `PortableLauncher/Game.zip`. Original photos/videos are not required. Git LFS and an Asset Store purchase are not required for the current checkout.

[Packages/manifest.json](Packages/manifest.json) uses Unity's built-in modules; Unity resolves them when opening the project. Do not install URP/HDRP or replace the input system to follow this guide. Generated `.sln` files, `Library/`, and a project-local `Tools/Unity/` editor installation are not included in Git.

In Hub, add this existing repository folder as a project and select **6000.3.25f1**. Open it once, let importing and compilation finish, and check the Console. To try the game, open `Assets/Scenes/Gotland.unity` and press Play. Close the editor before running the command-line build for this same checkout.

## 4. Build the Windows game

Run from the repository root. `Start-Process -Wait` waits for the Unity GUI executable to finish before checking its result.

```powershell
$projectRoot = (Get-Location).Path
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe'
New-Item -ItemType Directory -Force Logs | Out-Null
$buildArguments = '-batchmode -nographics -quit -buildTarget StandaloneWindows64 -projectPath "{0}" -executeMethod BuildGame.Build -logFile "{0}\Logs\build-windows.log"' -f $projectRoot
$build = Start-Process -FilePath $unity -ArgumentList $buildArguments -Wait -PassThru
if ($build.ExitCode -ne 0) { throw 'Unity build failed. Read Logs/build-windows.log.' }
Select-String -Path Logs/build-windows.log -Pattern 'TRACK_IMPORT_TEST passed','GOTLAND_BUILD_SUCCESS'
```

Alternatively, use **Gotland Ring → Build Windows x64** in the editor. [BuildGame.cs](Assets/Editor/BuildGame.cs) sets the platform, scripting backend, graphics API, and scene, then runs the track import checks before building.

The output is `Build/Windows/GotlandRing.exe` together with its data folder, Unity/Mono libraries, track data, and notices. Keep the whole `Build/Windows` folder together when using this raw build; copying just its EXE is insufficient.

```powershell
Start-Process -FilePath .\Build\Windows\GotlandRing.exe
```

## 5. Run the standalone checks

Close the normal game instance first. These tests need a desktop session and graphics; keep each test window active and let it finish. Do not add `-nographics` to player tests.

```powershell
$projectRoot = (Get-Location).Path
New-Item -ItemType Directory -Force Logs | Out-Null
foreach ($check in 'model-preview','smoke-test','settings-test','sign-test','autopilot-test') {
    $testArguments = '--{0} -screen-width 1600 -screen-height 900 -logFile "{1}\Logs\{0}-windows.log"' -f $check, $projectRoot
    $test = Start-Process -FilePath .\Build\Windows\GotlandRing.exe -ArgumentList $testArguments -Wait -PassThru
    if ($test.ExitCode -ne 0) { throw "$check test exited with code $($test.ExitCode)." }
}
Select-String -Path Logs/*-windows.log -Pattern 'RALLY_MODEL_TEST','SMOKE_TEST','BRAKE_TEST','SETTINGS_TEST','TRACK_SIGNS_TEST','AUTOPILOT_TEST ALL PASSED'
```

Inspect the logs as well as exit codes. Expected results are:

| Check | Success evidence |
|---|---|
| `--model-preview` | `RALLY_MODEL_TEST passed ...` and car/cockpit/paint screenshots. |
| `--smoke-test` | Driving statistics, then `BRAKE_TEST ... pass=True`. |
| `--settings-test` | `SETTINGS_TEST passed: cancel, apply, persistence, pause restoration`. |
| `--sign-test` | `TRACK_SIGNS_TEST passed: 42 boards ...`. |
| `--autopilot-test` | `AUTOPILOT_TEST ALL PASSED ...` after five laps and recovery checks. |

Also try **Ctrl+P** for autopilot, **Ctrl+F** for fullscreen, **F3** for settings, and **Escape** for pause. Screenshots from the raw Windows build are written beside `GotlandRing.exe`. See [VERIFICATION.md](VERIFICATION.md) for the checked behavior.

## 6. Make the portable EXE

Use **PowerShell 7**: the packaging script calls `System.IO.Path.GetRelativePath`, which Windows PowerShell 5.1's .NET Framework does not provide. Run packaging after rebuilding and testing the game so the launcher embeds the current files.

```powershell
.\scripts\Package-Game.ps1
dotnet publish PortableLauncher\PortableLauncher.csproj -c Release -o Build\Portable
if ($LASTEXITCODE -ne 0) { throw 'Portable launcher publish failed.' }
```

`Package-Game.ps1` creates `PortableLauncher/Game.zip` from `Build/Windows`. `dotnet publish` downloads the required .NET build/runtime packs on its first run and creates **`Build/Portable/GotlandRing-Portable.exe`**. The project file already selects `win-x64`, self-contained publishing, and a single-file executable.

Check extraction, then launch it:

```powershell
$extract = Start-Process -FilePath .\Build\Portable\GotlandRing-Portable.exe -ArgumentList '--extract-only' -Wait -PassThru
if ($extract.ExitCode -ne 0) { throw 'Portable extraction failed.' }
Start-Process -FilePath .\Build\Portable\GotlandRing-Portable.exe
```

Only the portable EXE needs to be copied to another Windows PC. It extracts its bundled game to `%LOCALAPPDATA%\Mannetroll\GotlandRing\<build hash>`. Players do not need Unity or .NET installed. This workflow produces an unsigned executable.

`PortableLauncher/Game.zip` is generated and ignored by Git. Rebuild it for every release, publish the launcher locally, and upload the tested EXE alongside the macOS ZIP and `SHA256SUMS`. The [release workflow](.github/workflows/release.yml) verifies those published downloads; see the [release steps](README.md#publish-a-release).

## 7. Optional release upload

Install GitHub CLI, authenticate, and set the tag of an existing release that matches your tested build:

```powershell
gh auth login
$releaseTag = 'v0.2.0'
gh release upload $releaseTag Build/Portable/GotlandRing-Portable.exe --repo mannetroll/GotlandRing
```

If that filename already exists, the command stops. Use `--clobber` only when intentionally replacing the release asset.

## Troubleshooting

| Symptom | Action |
|---|---|
| `Unity.exe` is not found | Locate the exact 6000.3.25f1 editor in Hub and update `$unity`. |
| Unity license or licensing-client error | Open Hub under your normal account, finish sign-in/license activation, then retry the build. |
| Project already open / locked | Close the editor using this checkout before starting a batch build. |
| Packaging reports no `GetRelativePath` method | Start `pwsh` and verify PowerShell 7. |
| PowerShell blocks the packaging script | Check the [execution policy](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy). On a personally managed machine, a session-scoped `Set-ExecutionPolicy -Scope Process RemoteSigned` permits local scripts; follow managed-device policy. Review and unblock a trusted downloaded script if needed. |
| `dotnet` missing or `NETSDK1045` | Install the .NET 10 **SDK**, open a fresh terminal, and check `dotnet --list-sdks`. |
| Launcher shows older behavior | Rebuild Unity, regenerate `PortableLauncher/Game.zip`, then publish the launcher again. |
| Missing track data, textures, or recording | Use the complete repository checkout and run builds from its root. |
| Runtime checks stall | Keep the test window active; the normal player pauses updates when in the background. Inspect its log for exceptions. |
