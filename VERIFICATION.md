# Verification

Verified on an Apple M1 Max running macOS 15.7.9 with Unity 6000.3.25f1.

## Builds

- macOS: `Build/macOS/GotlandRing.app`, native ARM64, Mono, Metal. The app and embedded libraries pass `codesign --verify --deep --strict` with Unity's local ad-hoc signature; this is not a notarized distribution.
- Windows: `Build/Windows/GotlandRing.exe`, x86-64, Mono, Direct3D 11. Cross-compilation succeeded on macOS. The Windows executable was not run in this verification, and the committed portable launcher payload was not regenerated.
- Both builds pass `TrackImportChecks`: 2,405 unique track points, approximately 7,214.397 m horizontal length, all-point height projection, malformed input rejection, and bounded local elevation smoothing.
- Both output folders include the source centerline, validation image, track documentation, and license/asset notices. Windows packaging reads only `Build/Windows`.

## macOS runtime

- `--smoke-test`: reached 24.0 m/s (86.4 km/h), 222.3 m displacement from the start, and 5,857 RPM after 30 seconds. Four seconds of braking reduced speed to 0.00 m/s (`pass=True`).
- Rendering sample: 119.4 FPS at 1600×900 using the Apple M1 Max Metal device. This is a short sample, not a sustained performance benchmark.
- `--settings-test`: cancel, apply, persistence, and pause restoration passed.
- Runtime tree-clearance check passed for 820 trees. No game exceptions or shader errors were reported in either runtime test.
- Captured cockpit and settings screenshots were inspected. macOS screenshots are written to `Application.persistentDataPath`, outside the signed app bundle.

Build and runtime logs are in the ignored `Logs/` folder. Commands to reproduce these checks are in `README.md`. These checks do not validate a full lap, calibrated vehicle dynamics, or exact acoustic fidelity.
