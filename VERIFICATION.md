# Gotland Ring v0.3.2 verification

Verified on 10 October 2026 with Unity 6000.3.25f1. The macOS ARM64 and Windows x64 packages use the same release source and report **v0.3.2**. Windows uses Direct3D 11; macOS uses Metal.

## Standalone checks

All **12 macOS suites** pass: feedback, AWD, arcade autopilot, settings, reference lap, scenery, surface contact, model animation, car switching, wind turbines, track signs and driving/braking smoke checks.

On Windows 11 (Fractal), all **six functional suites** pass: feedback, car switching and driver animation, AWD, arcade autopilot, settings and reference lap. Windows runs use `-batchmode -nographics`, validating simulation, skinned-mesh deformation, replay state, clock and controls without testing rendered appearance or sound output.

| Measurement | macOS | Windows 11 |
|---|---:|---:|
| AWD circuit autopilot lap | 255.61 s | 255.60 s |
| Track segments visited | 2,405 / 2,405 | 2,405 / 2,405 |
| Maximum speed | 181.7 km/h | 181.7 km/h |
| Minimum car-body clearance to asphalt edge | 0.44 m | 0.44 m |
| Maximum lap body sideslip above 43 km/h | 8.63° | 8.68° |
| Time above 3° sideslip in corners | 91.49 s | 91.53 s |
| Downshifts under power | 20 | 20 |
| Main-straight 0–100 km/h | 7.24 s | 7.29 s |
| Braking distance from approximately 100 km/h | 35.83 m | 35.98 m |

The 24 manual steering recovery cases finish with at most 0.50° residual sideslip and 1.11°/s residual yaw on both platforms. Eight full-throttle reversal runs and four countersteering recoveries pass. Circuit and training-pad physics remain shared. Start/finish checks require the ordered checkpoints and a forward finish crossing; reverse crossings, rocking across the line and recovery do not add laps.

## Cockpit and surface feedback

The steering checks cover the visible wheel, its centred column axis, road-wheel agreement, both directions, full locks, pause and return to centre. Both car models pass the driver checks: visible gloves follow their bones, elbows remain connected, the torso stays fixed, grip changes alternate, and pause/seek/centre poses are stable. Sweeping the wheel from −720° to +720° in 0.5° steps gives a maximum hand displacement of 0.0095 m per step and at least 0.070 m of arm reach remaining. Mac cockpit screenshots were reviewed at left/right turns and the reference-lap view shows the v0.3.2 label.

Feedback checks cover grounded, split-surface and airborne wheel contacts, stationary settling, acceleration, disabled sliders, mouse look, chase-camera isolation and recovery. Editor checks validate 711 kerb sample locations, asphalt/loose-ground boundaries, training-pad boundaries, braking/cornering direction, 30/60/120 Hz behaviour and motion limits.

Engine and surface-audio checks pass at 44.1, 48 and 96 kHz, including level/headroom, mute, zero intensity, stationary/airborne settling and sample-rate changes. Settings checks restore saved preferences after apply/cancel and movement/sound persistence checks.

## Replay and scenery

The reference-lap suite validates all 4,464 motion/audio samples, complete circuit coverage, clock boundaries, pause/mute/completion, eight driving-state restoration combinations, momentum, lap-time isolation, camera proportions and audio-listener cleanup. Feedback settings and steering animation also work in replay.

Scenery checks validate all **26 kerb runs / 13,812 vertices**, road sides, upward faces and a 0.025 m paint offset. Quarry mesh and driving-surface queries agree with collision geometry. Raised banks leave the road and shoulder clear. Surface checks cover forward/reverse contact, banking, recovery and the seam, with a maximum contact error of 0.0119 m.

## Build and package checks

Both builds pass track/surface, turbine, forest, car-import, ride-feedback and engine/surface-audio checks.

The macOS ZIP passes archive-integrity checking. All **147 app files** and **18 accompanying documentation/data files** match their sources. The extracted app reports v0.3.2, passes `codesign --verify --deep --strict`, and passes the feedback startup check.

The Windows portable EXE is built with .NET 10. Both launcher and game report v0.3.2. All **161 transferred runtime files** and **161 extracted payload files** match by SHA-256. The final portable EXE starts the game and reaches the settings-test success marker. Its retrieved checksum matches the Windows packaging result.

Both downloads include attribution and track/turbine data. Source films remain excluded. `SHA256SUMS` records the final package hashes.

## Reproduction and evidence

Run the standalone checks with the flags in [README.md](README.md). Core simulation suites support `-batchmode -nographics`; scenery material checks need graphics enabled. Each suite must exit successfully and report its success marker without failed assertions. See [AWD assumptions and controls](docs/AWD-PHYSICS.md).

Local evidence is outside Git in `Logs/release-0.3.2/` and `Build/Release-v0.3.2/`. Build logs are `Logs/build-macOS.log` and `Logs/build-windows-0.3.2.log`.

## Limits

Windows visual appearance and audio listening were not reviewed in the disconnected desktop session. Native resizing/fullscreen transitions were not repeated. Audio checks validate generated samples; they do not replace listening feedback.

These checks validate prototype behaviour, not calibrated Subaru dynamics or surveyed circuit geometry. The reference lap is a position-matched replay with estimated audio controls, not measured Subaru telemetry. Scenery collisions, damage, detailed differentials and tyre-temperature simulation remain incomplete or absent. Packages have no Windows publisher signature or Apple Developer ID/notarization; the Mac app is ad-hoc signed. Car-asset use remains noncommercial under the included notices.
