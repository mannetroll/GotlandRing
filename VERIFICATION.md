# Verification

Verified on an Apple M1 Max running macOS 15.7.9 with Unity 6000.3.25f1.

## Builds

- macOS: `Build/macOS/GotlandRing.app`, native ARM64, Mono, Metal. The app and embedded libraries pass `codesign --verify --deep --strict` with Unity's local ad-hoc signature; this is not a notarized distribution.
- Windows: `Build/Windows/GotlandRing.exe`, x86-64, Mono, Direct3D 11. Cross-compilation succeeded on macOS. The Windows executable was not run in this verification, and the committed portable launcher payload was not regenerated.
- Both builds pass `TrackImportChecks`: the bundled CSV matches `track/gotland_ring_full_centerline_3m_lowpass.csv`, with 2,405 unique points, approximately 7,214.398 m horizontal length / 7,216.638 m 3D length, minimum/maximum Y of −19.445 / 3.929 m, all-point height projection, and malformed input rejection. Unity's single-precision length accumulation reports 7,216.641 m in 3D.
- Both output folders include the low-pass source centerline, validation image, low-pass elevation profile, track documentation, and license/asset notices. The bundled resource and distributed CSVs match the source byte for byte. Windows packaging reads only `Build/Windows` and includes both track images.

## macOS runtime

- Window resizing was checked by dragging the corner from 1600×900 to 1200×680 and back. The HUD, pause menu and driving settings scale with the window; settings were opened with F3 at the smaller size.
- `--smoke-test`: reached 24.0 m/s (86.4 km/h), 222.3 m displacement from the start, and 5,857 RPM after 30 seconds. Four seconds of braking reduced speed to 0.00 m/s (`pass=True`).
- The runtime reads the low-pass CSV coordinates directly, with no additional height filtering. Imported elevation range is 23.374 m.
- Rendering sample with all 42 track signs: 120.0 FPS at 1600×900 using the Apple M1 Max Metal device. This is a short sample, not a sustained performance benchmark.
- Runtime tree-clearance check passed for 820 trees. No game exceptions or shader errors were reported in the driving test.
- The captured cockpit screenshot was inspected. macOS screenshots are written to `Application.persistentDataPath`, outside the signed app bundle.
- `--sign-test`: all 42 numbered name boards pass right-side placement, approach-facing orientation, text-fit and road/runoff-clearance checks. Minimum clearance from any track segment is 12.51 m, beyond the 11.5 m runoff boundary. Driver-view screenshots of signs 1, 3, 11, 31, 35 and 42 were inspected, including paired names and Swedish characters. Lettering respects scenery and car-body depth.
- The panoramic `Sky.hdr` texture is imported without mipmaps to prevent a vertical filtering seam at the longitude wrap. The sky is continuous in the six captured driver views; the corresponding render check is logged in `Logs/sky-macOS.log`.

Build and runtime logs are in the ignored `Logs/` folder. Commands to reproduce these checks are in `README.md`. These checks do not validate a full lap, calibrated vehicle dynamics, or exact acoustic fidelity.
