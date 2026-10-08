# Build Gotland Ring on macOS

This guide sets up an **Apple Silicon Mac** to edit the game, build `Build/macOS/GotlandRing.app`, and create the downloadable macOS ZIP. Run commands in Terminal from the repository root unless stated otherwise. For a Windows development machine, see [Unity_Win11_Setup.md](Unity_Win11_Setup.md).

The project pins **Unity 6000.3.25f1 LTS**, revision **e1dba0a9aba4**, in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt). Its Mac build uses **ARM64**, **Mono**, the **built-in renderer**, and **Metal**. The build script produces an Apple Silicon app, not an Intel or Universal app.

## 1. Tools and binaries to install

| Component | Required for | Download / selection |
|---|---|---|
| Apple Silicon Mac, M1 or newer | Native editor, game build and runtime checks | Use macOS **13 Ventura or newer** for Unity 6.3 development. The built game's deployment minimum is macOS **12**. |
| Unity Hub for macOS | Installing the editor and managing its license | [Unity Hub installation and downloads](https://docs.unity.com/en-us/hub/install-hub). |
| Unity Editor **6000.3.25f1**, macOS ARM64 | Compiling the game | [Exact editor release and installers](https://unity.com/releases/editor/whats-new/6000.3.25f1): choose **macOS ARM64** / Apple Silicon. |
| Mac standalone **Mono** player support | Building the app | Included with the Mac editor. **Mac Build Support (IL2CPP)** is a separate, unused module for this project. |
| Rosetta 2 | Unity editor toolchain prerequisites on Apple Silicon | Install through macOS; [Apple's Rosetta instructions](https://support.apple.com/en-us/102527). Unity lists it as a requirement even with its ARM64 editor. |
| Apple Command Line Tools and Git | Cloning/updating the repository and command-line development | Install with `xcode-select --install`; [Apple's Command Line Tools guide](https://developer.apple.com/documentation/xcode/installing-the-command-line-tools/). A configured full Xcode installation also supplies Git. |
| Bash, `ditto`, `codesign`, `file`, `plutil` | Existing build script, ZIP packaging and inspection | Included in macOS. No separate ZIP utility is needed. |
| A code editor, optional | Editing C# and project files | [Visual Studio Code](https://code.visualstudio.com/download), macOS Apple Silicon. |
| GitHub CLI, optional | Uploading release assets | [GitHub CLI](https://cli.github.com/), macOS installation options. |

Unity recommends at least 8 GB RAM; 16 GB or more is a practical development choice. Keep the checkout on a local SSD with room for the editor, imported `Library/` data, and builds. The editor's OS and Rosetta requirements are listed in [Unity 6.3 system requirements](https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html).

The app uses Unity's bundled C# compiler and Mono runtime. Building and packaging the macOS app does not require a separate .NET SDK, PowerShell, Homebrew, Python, Node.js, Java, Android SDK/NDK, or Blender. Full Xcode is not required for this Mono build; Unity requires it for **IL2CPP** builds, as documented in [macOS requirements](https://docs.unity3d.com/6000.3/Documentation/Manual/macos-requirements-and-compatibility.html).

## 2. Prepare command-line tools and Unity

Install Command Line Tools if they are not already available, and finish the installer before continuing:

```bash
xcode-select --install
```

Check the active tools and architecture:

```bash
xcode-select -p
git --version
uname -m
sw_vers -productVersion
command -v bash ditto codesign file plutil
```

Expect `arm64` from `uname -m`. Use a native Terminal session. If full Xcode is already selected, its developer directory is also a valid result from `xcode-select -p`.

Install Rosetta if it is not present, following the prompts and license terms:

```bash
softwareupdate --install-rosetta
```

The shipped game itself runs natively on ARM64; the Rosetta requirement above belongs to the Unity development environment.

1. Install Unity Hub and sign in to your Unity account.
2. Activate a valid license in Hub's license settings: Unity Personal if eligible, or your assigned paid license. Complete activation before running a batch build.
3. Install **6000.3.25f1**, selecting the **Apple Silicon / ARM64 editor**. Use the [exact release page](https://unity.com/releases/editor/whats-new/6000.3.25f1) if this version is absent from Hub's default list.
4. The native Mac Mono player comes with the editor. Android, iOS, Web, dedicated-server and IL2CPP modules are unnecessary for the native build.

The usual editor executable is:

```text
/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity
```

The repository's script also recognizes a manually installed editor at:

```text
Tools/Unity/6000.3.25f1/Unity.app/Contents/MacOS/Unity
```

`Tools/Unity` is ignored by Git and is not downloaded with the project. Use Hub for a fresh setup, or set `UNITY_EDITOR` to your existing editor's executable. See [Unity's editor installation guide](https://docs.unity.com/en-us/hub/install-editors).

## 3. Get the complete project

Choose a parent folder for the checkout, then run:

```bash
git clone https://github.com/mannetroll/GotlandRing.git
cd GotlandRing
cat ProjectSettings/ProjectVersion.txt
```

All required game assets are included: the scene, C# scripts, shaders, textures, sky, icon, engine recording, track CSVs and license notices. Original reference photos/videos and Asset Store downloads are not required. The current checkout does not require Git LFS.

[Packages/manifest.json](Packages/manifest.json) uses Unity's built-in modules, resolved by the editor. Keep the supplied built-in renderer and input configuration. `Library/`, `Logs/`, `Build/`, and the local editor are generated or installed locally.

Add this repository folder to Hub, open it with **6000.3.25f1**, and let asset importing and C# compilation finish. Open `Assets/Scenes/Gotland.unity` and press Play to try the editor version. Close the editor before the command-line build uses the same checkout.

## 4. Build the macOS app

```bash
./scripts/Build-macOS.sh
```

For a nonstandard editor location:

```bash
UNITY_EDITOR="/path/to/Unity.app/Contents/MacOS/Unity" ./scripts/Build-macOS.sh
```

The script selects the macOS target and calls `BuildGame.BuildMacOS`. [BuildGame.cs](Assets/Editor/BuildGame.cs) selects ARM64, Mono and Metal, prepares the scene, validates the track, and copies the app's accompanying data and notices.

Expected output and checks:

```bash
grep -E 'TRACK_IMPORT_TEST passed|GOTLAND_BUILD_SUCCESS' Logs/build-macOS.log
file "Build/macOS/GotlandRing.app/Contents/MacOS/Gotland Ring - Impreza"
codesign --verify --deep --strict Build/macOS/GotlandRing.app
open Build/macOS/GotlandRing.app
```

The executable should be reported as `Mach-O 64-bit executable arm64`. A successful `codesign --verify` normally prints no output. This verifies the local app signature; it does not establish Developer ID signing or notarization.

Alternatively, select macOS in Unity's Build Profiles and use **Gotland Ring → Build macOS Apple Silicon**.

## 5. Run the standalone checks

Close any normal game instance first. Run these tests sequentially with graphics enabled and keep each test window active until it exits. The `open -W` option waits for the app to close.

```bash
mkdir -p Logs
open -n -W Build/macOS/GotlandRing.app --args --model-preview -screen-width 1600 -screen-height 900 -logFile "$PWD/Logs/model-preview-macOS.log"
open -n -W Build/macOS/GotlandRing.app --args --smoke-test -screen-width 1600 -screen-height 900 -logFile "$PWD/Logs/smoke-macOS.log"
open -n -W Build/macOS/GotlandRing.app --args --settings-test -screen-width 1600 -screen-height 900 -logFile "$PWD/Logs/settings-macOS.log"
open -n -W Build/macOS/GotlandRing.app --args --sign-test -screen-width 1600 -screen-height 900 -logFile "$PWD/Logs/signs-macOS.log"
open -n -W Build/macOS/GotlandRing.app --args --autopilot-test -screen-width 1600 -screen-height 900 -logFile "$PWD/Logs/autopilot-macOS.log"
grep -E 'RALLY_MODEL_TEST|SMOKE_TEST|BRAKE_TEST|SETTINGS_TEST|TRACK_SIGNS_TEST|AUTOPILOT_TEST ALL PASSED' Logs/*-macOS.log
```

Read the player logs to establish the result; a successful `open` command alone does not prove a test passed. Do not add `-nographics` to these runtime tests.

| Check | Success evidence |
|---|---|
| `--model-preview` | `RALLY_MODEL_TEST passed ...` and car/cockpit/paint screenshots. |
| `--smoke-test` | Driving statistics followed by braking to approximately zero, `pass=True`. |
| `--settings-test` | `SETTINGS_TEST passed: cancel, apply, persistence, pause restoration`. |
| `--sign-test` | `TRACK_SIGNS_TEST passed: 42 boards ...`. |
| `--autopilot-test` | `AUTOPILOT_TEST ALL PASSED ...` after five laps and recovery checks. |

Try **Cmd+P**, **Cmd+F**, **Escape**, and **F3** in a normal run. F2/F3 may require Fn on a Mac keyboard. Screenshots are saved in:

```text
~/Library/Application Support/com.Mannetroll-Solutions-AB.Gotland-Ring---Impreza/
```

The screenshot paths also appear in the logs. See [VERIFICATION.md](VERIFICATION.md) for the tested behavior.

## 6. Make the downloadable ZIP

After the build and checks pass:

```bash
./scripts/Package-macOS.sh
shasum -a 256 Build/GotlandRing-macOS-arm64.zip
```

The script verifies the app's signature and creates **`Build/GotlandRing-macOS-arm64.zip`**. It contains a `GotlandRing-macOS-arm64/` folder with the app, README, license/asset notices, source track CSV and validation images. It uses macOS `ditto` to preserve the app's executable permissions.

Players extract the ZIP and open `GotlandRing.app`; Unity does not need to be installed. Keep the app and accompanying data/notices together when distributing the build.

The current package is locally signed, without Developer ID signing or notarization. For opening a downloaded copy, see [Apple's app-opening instructions](https://support.apple.com/en-us/102445). Developer ID distribution is a separate workflow requiring an Apple Developer account, an appropriate signing identity, and Apple's signing/notarization tools; the repository scripts do not perform it. See [Apple's macOS distribution guide](https://developer.apple.com/developer-id/).

## 7. Optional release upload

Install GitHub CLI using its [macOS installation instructions](https://cli.github.com/), authenticate, and choose an existing release tag matching the tested game:

```bash
gh auth login
release_tag=v0.2.0
gh release upload "$release_tag" Build/GotlandRing-macOS-arm64.zip --repo mannetroll/GotlandRing
```

An existing asset with the same filename causes the upload to stop. Add `--clobber` only when intentionally replacing it. The repository's GitHub Actions workflow builds the Windows launcher; it does not build or upload the Mac app.

## Optional: build the Windows player from this Mac

This is separate from the native macOS build. Install **Windows Build Support (Mono)** for **6000.3.25f1** using Hub's module management or the **macOS ARM64** component list on the [exact release page](https://unity.com/releases/editor/whats-new/6000.3.25f1). The module's local ID is `windows-mono`.

With that module installed and the editor closed:

```bash
mkdir -p Logs
unity_editor="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity"
"$unity_editor" -batchmode -nographics -quit \
  -buildTarget StandaloneWindows64 -projectPath "$PWD" \
  -executeMethod BuildGame.Build -logFile "$PWD/Logs/build-windows.log"
```

Adjust `unity_editor` if using the local `Tools/Unity` installation. The result is `Build/Windows/GotlandRing.exe` and its runtime files. It requires Windows for native runtime testing. Follow [Unity_Win11_Setup.md](Unity_Win11_Setup.md) on Windows for the supported portable-launcher packaging and test workflow; .NET 10 and PowerShell 7 are not prerequisites for the native Mac app.

## Troubleshooting

| Symptom | Action |
|---|---|
| Editor not found | Install the exact ARM64 editor in Hub or set `UNITY_EDITOR` to its executable. |
| Missing x86 tool / bad CPU type | Install Rosetta as required by the Unity 6.3 editor toolchain and check that the editor itself is the ARM64 build. |
| Unity license error | Finish sign-in and license activation in Hub under the same user account. |
| Package-manager socket `EPERM` or read-only cache | Run the build from a normal Terminal session with access to Unity's user caches and local IPC. A restricted automation sandbox may need permission to launch Unity. |
| Project already open / locked | Close the editor using this checkout before the batch build. |
| `Permission denied` on a script | A Git clone preserves script modes. For a source ZIP, run `chmod +x scripts/Build-macOS.sh scripts/Package-macOS.sh`. |
| Signature verification fails | Rebuild the app and avoid editing files inside the generated signed app bundle. |
| Missing Windows target | Install **Windows Build Support (Mono)** for the exact editor version if cross-building Windows. |
| Runtime tests stop progressing | Keep their window active and inspect the player log. Build-time `-nographics` is not appropriate for these checks. |
