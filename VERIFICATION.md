# Gotland Ring v0.3.4 verification

Verified on 31 October 2026 with Unity 6000.3.25f1. macOS uses Apple Silicon ARM64 and Metal on an Apple M1 Max. The Windows x64 player is built natively on Fractal, running Windows 11 Pro build 26300, and uses Direct3D 11 during normal play. The portable launcher uses .NET 10 and embeds its runtime. Both game and launcher report **v0.3.4**.

## Standalone checks

All **16 macOS suites** pass with graphics enabled: instruments, keyboard setup, menu/HUD preview, AWD, arcade autopilot, settings, reference lap, feedback, car switching, scenery views, scenery geometry, surface contact, wind turbines, model animation, track signs and driving/braking smoke checks.

All **nine Windows functional suites** pass without graphics: instruments, keyboard setup, feedback, car switching, AWD, arcade autopilot, settings, reference lap and scenery views. Headless instrument and keyboard checks explicitly skip GPU captures while retaining their functional assertions.

The build-time checks pass on both platforms: track/surface data, wind turbines, forest placement, car imports, driver geometry, ride feedback, engine audio and surface audio. Source-text hashes, normalized for Windows line endings, match for the scripts, shaders, package manifests, Unity version and portable launcher project.

## Instruments, controls and menus

The instrument checks validate both models in AWD and arcade driving, sideways speed, reverse, pause and independent display state. Mac GPU readback verifies the analogue tachometer, speedometer and fuel needle positions. Replay checks verify instrument data after seeking, pause and completion.

Keyboard checks cover conflicting assignments, cancellation, persistence across reloads, alternate movement keys, clearing bindings, reserved Escape, default restoration and current on-screen hints. Headless checks finish after the binding assertions; Mac runs also capture both help pages.

The Mac menu/HUD preview captures driving, bonnet instruments, mouse steering, autopilot, pause menu, AWD/arcade settings, view/sound, drone setup, keyboard setup and the training area. It covers the normal window size and 1280×720. Captures are visually inspected. The README preserves the front three-quarter main view with cockpit and pause-menu images below it; all are actual v0.3.4 captures.

## Driving and scenery

The AWD suite covers engine/clutch response, controls, mouse steering, lap timing, physical contact, banking, training-area transitions, manual recovery, power slides and a complete circuit lap. On macOS the lap visits all **2,405 segments**, takes **262.41 seconds**, reaches **177.0 km/h**, and retains **0.51 m** minimum body clearance to the asphalt edge. The main-straight 0–100 km/h check takes **7.52 seconds**; braking from approximately 100 km/h takes **35.72 m**. There are no rapid autopilot brake pulses in the lap check.

Arcade autopilot checks complete five laps across default and extreme handling settings, then verify off-road/reverse recovery. Training checks cover AWD/arcade, manual/autopilot, pause, recovery and lap isolation. Engine/slide checks include 12 held-throttle cases, 18 turnaround cases, 24 manual stability cases, eight full-throttle reversal runs and eight lift-off recoveries.

Replay checks cover all **4,464 samples**, eight restoration combinations, timing, momentum, camera layout, audio-listener cleanup and pause/mute/completion. Feedback checks cover grounded, split-surface and airborne wheel contacts, disabled effects, stationary settling and camera isolation.

Scenery checks cover all 15 elevated views and saved-drive restoration, 26 kerb runs and 13,812 vertices, mapped building/barrier placement and 12 wind turbines. Maximum surface contact error on macOS is **0.0119 m**. Model checks validate both cars, wheel animation, cockpit visibility and driver hands.

## Packages

The macOS ZIP is extracted and checked for archive integrity. Every app file and accompanying documentation/data file is compared with its source using SHA-256. The extracted app reports v0.3.4, passes `codesign --verify --deep --strict`, and is launched for an instrument check.

The Windows portable EXE is built on Fractal. Its embedded payload is extracted to a fresh content-hash directory, and every packaged runtime file is compared with the built source by SHA-256. The launcher and Unity game version are checked independently. Launching the final portable EXE must reach `SETTINGS_TEST passed`.

`SHA256SUMS` records the Windows EXE and macOS ZIP hashes. The published downloads are fetched again and verified against those hashes. Local evidence is in `Logs/release-0.3.4/` and `Build/Release-v0.3.4/`, outside Git.

## Limits

No separate Windows 10 machine was available. The same Windows x64 package is provided for Windows 10/11, with runtime testing performed on Windows 11 only.

Fractal has no logged-in interactive desktop. The attempted Direct3D capture run could not set a window resolution; Windows rendered appearance, GPU instrument needles, native window resizing/fullscreen and audio listening were not approved by that run. Mac Metal captures cover the visible changes. Audio checks validate generated samples and do not replace listening feedback.

These checks validate prototype behaviour, not calibrated Subaru dynamics or surveyed circuit geometry. Packages have no Windows publisher signature or Apple Developer ID/notarization; the Mac app is ad-hoc signed. Car-asset use remains noncommercial under the included notices.
