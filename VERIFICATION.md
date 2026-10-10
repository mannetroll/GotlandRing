# Gotland Ring 0.3.0 verification

Verified on 10 October 2026 with Unity 6000.3.25f1. Fresh macOS ARM64 and Windows x64 builds use the same release source and report version 0.3.0.

## Native driving checks

The standalone release suites pass on Apple Silicon macOS and Windows 11. These runs use `-batchmode -nographics`; they validate simulation and controls, not rendered appearance or frame rate.

| Suite | macOS | Windows 11 |
|---|---|---|
| AWD contact, banking, drivetrain, power slides and full lap | Pass | Pass |
| Training area, shared physics, reset and circuit return | Pass | Pass |
| Mouse steering, independent pedals and autopilot priority | Pass | Pass |
| 24 manual steering recovery cases | Pass | Pass |
| Eight full-throttle reversals and four countersteering recoveries | Pass | Pass |
| Arcade racing line, five laps and off-road recovery | Pass | Pass |
| Settings apply/cancel, persistence and pause restoration | Pass | Pass |

Training and input checks are included in the AWD suite. The settings suite restores the user's saved preferences after checking both handling modes and the tyre-squeal checkbox.

## AWD measurements

| Measurement | macOS | Windows 11 |
|---|---:|---:|
| Circuit autopilot lap | 258.38 s | 258.33 s |
| Track segments visited | 2,405 / 2,405 | 2,405 / 2,405 |
| Maximum speed | 181.7 km/h | 181.7 km/h |
| Minimum car-body clearance to asphalt edge | 0.44 m | 0.45 m |
| Maximum lap body sideslip above 43 km/h | 8.64° | 8.63° |
| Time above 3° sideslip in corners | 93.17 s | 93.05 s |
| Downshifts under power | 21 | 21 |
| Main-straight 0–100 km/h | 7.27 s | 7.29 s |
| Braking distance from approximately 100 km/h | 35.79 m | 35.92 m |

On the flat training pad, full-throttle steering reversals every 0.8 seconds reach approximately 37° body sideslip at 100 km/h and 54° at 140 km/h, with all four tyre contacts on asphalt. Quicker 0.4-second reversals reach approximately 13° and 16°. Both directions pass. Four separate checks lift and countersteer from a 20° slide and recover below 0.03° sideslip and 0.10°/s yaw rate, to displayed precision.

The 24 manual steering cases cover 60, 100 and 140 km/h, both directions, throttle on/off, a steering pulse and a reversal. Maximum residual sideslip is 0.50° and maximum residual yaw is 1.11°/s. These tests use the same vehicle controller on both driving areas.

## Build and package checks

Both native builds pass the track/surface, turbine, forest, car-import and engine/tyre-audio checks. The source surface CSV is preserved, both car models are prepared and the bundled data/attribution files are included.

The macOS ZIP passes archive-integrity checking. All 147 extracted app files and 18 accompanying documentation/data files match their sources by SHA-256 or byte comparison. The extracted app reports version 0.3.0 and passes `codesign --verify --deep --strict`.

The Windows portable EXE is built with .NET 10 and reports version 0.3.0 for both launcher and game. All 161 transferred runtime files match before testing, and all 161 extracted payload files match the package inputs by SHA-256. Launching the final portable EXE also reaches the game's settings-test success marker. The retrieved EXE matches the Windows packaging checksum.

Both downloads include attribution and track/turbine data, with source films excluded. `SHA256SUMS` records the final package hashes.

## Reproduction and evidence

Run the built game with `--awd-test`, `--autopilot-test` and `--settings-test`, adding `-batchmode -nographics -logFile <path>`. Each suite reports a success marker and exits; failed assertions or a nonzero exit invalidate the run. Physics steps are batched between frames for the full-lap checks. See [AWD assumptions and controls](docs/AWD-PHYSICS.md) and [build/package commands](README.md).

Release evidence is kept outside Git in `Logs/release-0.3.0/` and `Build/Release-v0.3.0/`. Build logs are `Logs/build-macOS.log` and `Logs/build-windows-0.3.0.log`.

## Limits

These measurements validate the prototype's behaviour, not calibrated Subaru tyre dynamics or surveyed circuit geometry. AWD uses individual wheel contacts and suspension forces; F4 selects a separate arcade comparison. Scenery collisions, damage, detailed differentials and tyre-temperature simulation are incomplete or absent.

The release checks did not repeat visual screenshot review, native window resizing, fullscreen transitions or Windows audio listening. Packages have no Developer ID/notarization or Windows publisher signature; the macOS app uses local ad-hoc signing. Car-asset use remains noncommercial under the included notices.
