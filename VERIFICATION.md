# Gotland Ring v0.3.1 verification

Verified on 10 October 2026 with Unity 6000.3.25f1. The macOS ARM64 and Windows x64 packages use the same release source and report **v0.3.1**. Windows uses Direct3D 11; macOS uses Metal.

## Standalone checks

All 11 macOS suites pass: AWD, arcade autopilot, settings, reference lap, scenery, surface contact, model animation, car switching, wind turbines, track signs and driving/braking smoke checks. The AWD suite includes training-area switching, mouse controls, 24 manual steering recovery cases, eight full-throttle reversal runs, four countersteering recoveries and start/finish timing.

On Windows 11, all four functional suites pass: AWD, arcade autopilot, settings and reference lap. These Windows runs use `-batchmode -nographics`, validating simulation, replay state, clock and controls without testing rendered appearance or sound output.

| Measurement | macOS | Windows 11 |
|---|---:|---:|
| AWD circuit autopilot lap | 255.61 s | 255.60 s |
| Track segments visited | 2,405 / 2,405 | 2,405 / 2,405 |
| Maximum speed | 181.7 km/h | 181.7 km/h |
| Minimum car-body clearance to asphalt edge | 0.44 m | 0.44 m |
| Maximum lap body sideslip above 43 km/h | 8.62° | 8.63° |
| Time above 3° sideslip in corners | 91.59 s | 91.68 s |
| Downshifts under power | 20 | 20 |
| Main-straight 0–100 km/h | 7.28 s | 7.19 s |
| Braking distance from approximately 100 km/h | 35.87 m | 35.97 m |

The 24 steering recovery cases finish with at most 0.50° residual sideslip and 1.11°/s residual yaw. The circuit and training pad use the same AWD controller. Settings checks restore saved preferences after exercising apply/cancel, both handling modes and tyre-squeal persistence.

## Reference lap and scenery

The reference-lap suite validates all 4,464 motion/audio samples, complete circuit coverage, clock boundaries, pause/mute/completion, eight driving-state restoration combinations, momentum, lap-time isolation, camera proportions and audio-listener cleanup. The extracted macOS ZIP also passes this suite with visible rendering; its Arho screenshot was reviewed for both views, clock, kerbs, credits and the **v0.3.1** label.

Mac scenery checks validate all **26 kerb runs / 13,812 vertices**, correct road sides, upward faces and a 0.025 m paint offset. Quarry mesh and driving-surface queries agree with collision geometry, and raised banks leave the road and shoulder clear. There are **10,595 trees** after the quarry clearing. Surface checks validate forward/reverse ground contact, banking, recovery and the seam, with a maximum contact error of 0.0119 m.

Lap checks require a complete circuit through ordered checkpoints and a forward crossing at the Gutemålrakan finish. Reverse crossings, rocking across the line and recovery do not add laps; restart uses the new finish location.

## Build and package checks

Both Unity builds pass their track/surface, turbine, forest, car-import and engine/tyre-audio checks.

The macOS ZIP passes archive-integrity checking. All **147 app files** and **18 accompanying documentation/data files** match their sources. The extracted app reports v0.3.1 and passes `codesign --verify --deep --strict`.

The Windows portable EXE is built with .NET 10. Both launcher and game report v0.3.1. All **161 transferred runtime files** and **161 extracted payload files** match by SHA-256. Launching the final portable EXE reaches the game's settings-test success marker. Its retrieved checksum matches the Windows packaging result.

Both downloads include attribution and track/turbine data. Source films remain excluded. `SHA256SUMS` records the final package hashes.

## Reproduction and evidence

Run standalone checks with the flags in [README.md](README.md). Core simulation suites support `-batchmode -nographics`; scenery material checks need graphics enabled. Each suite must exit successfully and report its success marker without failed assertions. See [AWD assumptions and controls](docs/AWD-PHYSICS.md).

Local release evidence is outside Git in `Logs/release-0.3.1/` and `Build/Release-v0.3.1/`. Build logs are `Logs/build-macOS.log` and `Logs/build-windows-0.3.1.log`.

## Limits

A separate Windows Direct3D visual run could initialize the graphics device but could not open the requested display in Fractal's disconnected desktop session. Windows visual review and audio listening were not completed. Mac replay appearance was reviewed; native resizing/fullscreen transitions were not repeated for this release.

These checks validate prototype behaviour, not calibrated Subaru dynamics or surveyed circuit geometry. The reference lap is a position-matched replay with estimated audio controls, not measured Subaru telemetry. Scenery collisions, damage, detailed differentials and tyre-temperature simulation remain incomplete or absent. Packages have no Windows publisher signature or Apple Developer ID/notarization; the Mac app is ad-hoc signed. Car-asset use remains noncommercial under the included notices.
