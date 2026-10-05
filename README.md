# Gotland Ring / Impreza

**A red Impreza. A Baltic circuit. An open practice session.**

[Download Windows EXE](https://github.com/mannetroll/GotlandRing/releases/download/v0.1.0/GotlandRing-Portable.exe) Â· [Release v0.1.0](https://github.com/mannetroll/GotlandRing/releases/tag/v0.1.0)

![Updated Impreza](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.1.0/docs/impreza.png)

![Cockpit view on Gotland Ring](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.1.0/docs/cockpit.png)

A small Unity driving prototype inspired by personal photographs and onboard footage of a **Subaru Impreza 2000 GT 2.0 S** at **Gotland Ring**. Drive a hand-traced, approximately 7.3 km circuit through open limestone scenery, with the red bonnet and scoop ahead of you.

## Get behind the wheel

Download **GotlandRing-Portable.exe**, then double-click it. Windows x64 and working graphics drivers are required. No Unity or .NET installation is needed. The download is approximately **71 MB**.

The first launch extracts the bundled game to `%LOCALAPPDATA%\Mannetroll\GotlandRing\<build hash>`. Later launches reuse those files. Allow about 200 MB for the EXE and extracted runtime. Copy only the portable EXE when moving to another computer. It is unsigned.

| Control | Action |
|---|---|
| W / Up arrow | Accelerate |
| S / Down arrow | Brake |
| A / D or Left / Right arrows | Steer |
| X | Reverse (brakes before changing direction) |
| F3 | Configure and save driving dynamics |
| Mouse | Turn your head |
| Right mouse | Center your view |
| C | Cockpit / bonnet / chase camera |
| R | Recover to the nearest track point |
| Home | Restart at the start line |
| Escape | Pause / resume and release the mouse |
| M | Mute / unmute |
| F2 | Save a screenshot alongside the extracted game |

## Inside the prototype

- Red bodywork, hood scoop, rear wing and three-gauge dashboard pod based on the reference car.
- Full circuit hand-traced from the photographed Gotland Ring sign, smoothed and scaled to roughly 7.3 km.
- Blue-and-white kerbs, limestone runoff, pines, pit wall and wind turbines inspired by the onboard footage.
- Automatic five-speed transmission, turbo boost, speed-sensitive steering and slower travel off the asphalt.
- Speed/RPM/boost display, minimap and checkpoint-gated lap timing.
- Synthesized boxer pulses, turbo lift-off and tire/wind layers, blended with a filtered three-second engine recording from the supplied footage.

## What v0.1.0 is

A playable first prototype, with simplified geometry and a ground-following bicycle handling model. Track elevations, widths and vehicle response are approximate; this is not a surveyed circuit or calibrated simulator. Scenery collisions, damage, AI opponents and full suspension physics are not implemented. Audio is inspired by the recording rather than an exact exhaust reproduction.

The original photographs and videos stay outside the repository. The derived engine sample required by the game is included.

## Build it

**Game:** Unity **6000.3.25f1 LTS**, Windows x64, Mono, built-in renderer / Direct3D 11.
**Portable launcher:** .NET **10**, self-contained Windows x64.

Open this folder in Unity. The scene is `Assets/Scenes/Gotland.unity`.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe' -batchmode -nographics -projectPath $PWD -executeMethod BuildGame.Build -quit -logFile build.log
```

After rebuilding the game, refresh the embedded payload and publish the launcher:

```powershell
.\scripts\Package-Game.ps1
dotnet publish PortableLauncher\PortableLauncher.csproj -c Release -o dist
```

The release workflow compiles the launcher around the **committed, tested Unity payload** in `PortableLauncher/Game.zip`. It does not rebuild Unity in CI or require a Unity license on the GitHub runner. Regenerate that payload whenever the Unity game changes.

| Location | Purpose |
|---|---|
| `Assets/Scripts/RingDrive.cs` | Circuit, scenery, car, handling, controls and HUD |
| `Assets/Scripts/ImprezaModel.cs` | Detailed procedural car geometry |
| `Assets/Scripts/DrivingSettings.cs` | Saved handling configuration |
| `Assets/Scripts/BoxerAudio.cs` | Responsive engine synthesis and recording layer |
| `Assets/Editor/BuildGame.cs` | Scene generation and Windows build |
| `PortableLauncher/` | Single-file launcher and game payload |
| `.github/workflows/release.yml` | Tagged GitHub release and EXE upload |

## Verification

The refreshed build passed configuration save/cancel, persistence and pause-restoration checks. The portable EXE was rebuilt and its embedded Impreza icon verified. Fresh screenshots above were captured from the current portable build.

### Earlier driving smoke test

The portable EXE was extracted and run on the Windows RTX 3090 machine. Automated driving reached **24.0 m/s (86.4 km/h)** and approximately **578 m** displacement; the braking test stopped the car at **0.00 m/s**. Runtime logs showed no game exceptions in the corrected build. This is a smoke test, not a full-lap handling validation.

Mannetroll Solutions AB

Steering is tuned for keyboard play: faster turn-in and centering, a wider steering range, and stronger asphalt grip. These are arcade-friendly settings rather than measured Impreza tire limits.

The remodeled Impreza uses a continuous curved body mesh with wheel-arch openings, rounded bumpers and scoop, detailed alloys, tinted side/rear glazing, and circular cockpit instruments.

### Driving dynamics

Press **F3**, or click **Dynamics**, to pause and configure cornering grip, side-slip recovery, low/high-speed steering angle, steering response, acceleration, braking and off-road grip. **Apply & close** saves settings locally between launches; **Cancel** discards edits. **Restore defaults** resets the draft until Apply. Default road cornering grip is now 28 m/s² for forgiving arcade handling. The body has denser curved surfaces and fuller fenders, with 96-segment tires.

Hold **X** to reverse (maximum approximately 29 km/h). Changing between forward and reverse first brakes the car; S/down remains the brake.
