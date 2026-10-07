# v0.1.4 verification

Verified on 7 October 2026 with Unity 6000.3.25f1. Both platform builds and all five standalone test suites passed with the high-resolution Impreza and mapped forest.

## Platforms and builds

- **macOS:** ARM64, Mono, Metal; Apple M1 Max, macOS 15.7.9. Output: `Build/macOS/GotlandRing.app`.
- **Windows 11:** x64, Mono, Direct3D 11; NVIDIA GeForce RTX 3090 on `fractal`. The game was built using the Mac editor's Windows Build Support and tested in the signed-in Windows desktop session. Output: `Build/Windows/GotlandRing.exe`. The portable launcher is built on Windows with .NET SDK 10.0.401.
- Both game versions are 0.1.4. Steering response defaults to **5** in the shared settings class; existing saved preferences are retained. F3 → Restore defaults → Apply selects the defaults for an existing profile.
- Both builds pass track import checks: 2,405 unique points, 7,214.398 m horizontal length, 7,216.638 m double-precision 3D length, and elevations from −19.445 to 3.929 m. Unity's single-precision 3D length is 7,216.641 m. The resource CSV matches the supplied low-pass CSV byte for byte; malformed input rejection and all-point height projection pass.
- Car preparation passes: 89 meshes, 18 materials, 4.32 m body length and 0.339 m wheel radius. The cockpit contains one driver with a separately hidden head.
- Forest preparation passes: 10,710 trees in 143 render tiles, with at least 19 m canopy clearance from the road. Northern and southern woodland coverage and tree dimensions pass import validation.

## Standalone suites

| Check | macOS | Windows 11 |
|---|---|---|
| `--model-preview` | Passed | Passed |
| `--smoke-test` | Passed | Passed |
| `--settings-test` | Passed | Passed |
| `--sign-test` | Passed | Passed |
| `--autopilot-test` | Passed | Passed |

Each process exited with code 0 and its expected success marker. No game exceptions, shader errors, failed assertions or `pass=False` results appeared in these ten logs.

- **Model:** forward/reverse wheel rotation, steering axes, stable axle centres, paused animation, cockpit visibility and unchanged physics root pass. Captures cover cockpit, bonnet, chase, front/rear, steering and all three paint colours. Cockpit, chase, paint and woodland rendering were visually inspected on both platforms; the co-driver and pace-note book are absent.
- **Driving and braking:** both players reached 24.0 m/s after 30 seconds. macOS travelled 222.3 m at 5,857 RPM; Windows travelled 222.1 m at 5,839 RPM. Four seconds of braking brought both to 0.00 m/s.
- **Settings:** cancel, apply/save, persistence and pause restoration pass without replacing the original saved setup.
- **Signs:** all 42 boards pass placement, approach-facing orientation, text-fit and clearance checks. Minimum clearance is 12.51 m. Captures include Swedish lettering and paired sign names.
- **macOS keyboard check:** T changed red to rally blue, C selected the cockpit, and R recovered the car without resetting its elapsed lap time. F3 and Escape opened/closed the settings dialog; fullscreen and windowed modes were reached. Native edge/corner resizing and exact window-size restoration were not revalidated in this run.

## Full-lap autopilot

Both platforms pass five laps using the normal 0.01-second physics step batched between rendered frames. Times below are simulated driving times, not wall-clock test durations. All four scenarios visit every one of the 2,405 track segments and exercise braking plus left and right steering.

| Scenario | Laps | Best lap | Maximum speed | Maximum centreline deviation |
|---|---:|---:|---:|---:|
| Defaults, steering response 5 | 2 | 180.30 s | 217.9 km/h | 1.34 m |
| Low grip / weak brakes | 1 | 248.28 s | 217.0 km/h | 0.94 m |
| High power / slow steering | 1 | 154.68 s | 233.1 km/h | 2.28 m |
| Saved settings | 1 | 185.50 s | 217.9 km/h | 1.31 m |

Pause, autopilot toggle/state and reverse/off-road recovery also pass. Recovery ends 0.16 m from the centreline at 35.04 m/s on both platforms.

## Rendering samples and limits

The ordinary driving smoke test sampled **115.5 FPS** on the M1 Max and **32.0 FPS** on the RTX 3090, both at 1600×900. Windows ran in an active Remote Desktop session, which can affect presentation rate. These short samples are not sustained performance benchmarks or a GPU comparison. The batched autopilot test's frame rate measures accelerated simulation work and should not be read as normal driving performance.

The tests do not establish calibrated vehicle dynamics, exact sound fidelity, surveyed scenery accuracy, or exhaustive manual input/resize coverage. Both platforms use the same source defaults, physics and scenery. Commands to reproduce the standalone checks are in [README.md](README.md).

## Evidence

Ignored local evidence is under `Build/Release-v0.1.4/`: `macOS-tests/results.json`, `Windows-tests/results.json` and per-suite screenshots. Logs are `Logs/v0.1.4-macOS-*.log` and `Logs/v0.1.4-Windows-*.log`. The current screenshots in `docs/autopilot.png` and `docs/chase.png` come from these verified players.

## Release packages

- **macOS ZIP:** integrity check passes; all 147 app files match the tested app after extraction. Included README, track CSV, both track images and attribution/license notices match the source files. Extracted and installed app signatures pass `codesign --verify --deep --strict`. The app is locally ad-hoc signed, not Developer ID signed or notarized. `/Applications/GotlandRing.app` contains this tested 0.1.4 build.
- **Windows portable EXE:** .NET publish succeeds. Running `--extract-only` on Windows succeeds, creates the expected build-hash cache and ready marker, and all 155 extracted files match the tested game and current packaged documentation by SHA-256. Unity player data contains game version 0.1.4 and the launcher version is 0.1.4. The EXE includes its .NET runtime and requires no .NET installation.
- SHA-256 checksums for both downloadable artifacts are distributed as `SHA256SUMS`. The release workflow downloads both artifacts, checks these hashes and tests macOS ZIP integrity.

Package audit evidence: `Build/Release-v0.1.4/macOS-package.json` and `Windows-package.json` (ignored).
