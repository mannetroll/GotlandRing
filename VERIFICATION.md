# Verification

Verified on an Apple M1 Max running macOS 15.7.9 with Unity 6000.3.25f1.

## Builds

- macOS: `Build/macOS/GotlandRing.app`, native ARM64, Mono, Metal. The app and embedded libraries pass `codesign --verify --deep --strict` with Unity's local ad-hoc signature; this is not a notarized distribution.
- Windows: `Build/Windows/GotlandRing.exe`, x86-64, Mono, Direct3D 11. Cross-compilation succeeded on macOS. The Windows executable was not run in this verification, and the committed portable launcher payload was not regenerated.
- Both builds pass `TrackImportChecks`: the bundled CSV matches `track/gotland_ring_full_centerline_3m_lowpass.csv`, with 2,405 unique points, approximately 7,214.398 m horizontal length / 7,216.638 m 3D length, minimum/maximum Y of −19.445 / 3.929 m, all-point height projection, and malformed input rejection. Unity's single-precision length accumulation reports 7,216.641 m in 3D.
- Both output folders include the low-pass source centerline, validation image, low-pass elevation profile, track documentation, and license/asset notices. The bundled resource and distributed CSVs match the source byte for byte. Windows packaging reads only `Build/Windows` and includes both track images.

## macOS runtime

- Native macOS window resizing preserves the game area's 16:9 aspect ratio, excluding the title bar. Corner, side and bottom-edge drags were checked at approximately 1428×804, 1202×676 and 1596×898 (AppKit rounds to whole display points). The HUD, pause menu and driving settings retain their proportions; F3 and Escape were checked at the resized dimensions.
- `--smoke-test`: reached 24.0 m/s (86.4 km/h), 222.3 m displacement from the start, and 5,857 RPM after 30 seconds. Four seconds of braking reduced speed to 0.00 m/s (`pass=True`).
- The runtime reads the low-pass CSV coordinates directly, with no additional height filtering. Imported elevation range is 23.374 m.
- Rendering sample with all 42 track signs: 120.0 FPS at 1600×900 using the Apple M1 Max Metal device. This is a short sample, not a sustained performance benchmark.
- Runtime tree-clearance check passed for 820 trees. No game exceptions or shader errors were reported in the driving test.
- The captured cockpit screenshot was inspected. macOS screenshots are written to `Application.persistentDataPath`, outside the signed app bundle.
- `--sign-test`: all 42 numbered name boards pass right-side placement, approach-facing orientation, text-fit and road/runoff-clearance checks. Minimum clearance from any track segment is 12.51 m, beyond the 11.5 m runoff boundary. Driver-view screenshots of signs 1, 3, 11, 31, 35 and 42 were inspected, including paired names and Swedish characters. Lettering respects scenery and car-body depth.
- The panoramic `Sky.hdr` texture is imported without mipmaps to prevent a vertical filtering seam at the longitude wrap. The sky is continuous in the six captured driver views; the corresponding render check is logged in `Logs/sky-macOS.log`.

Build and runtime logs are in the ignored `Logs/` folder. Commands to reproduce these checks are in `README.md`. These checks do not validate a full lap, calibrated vehicle dynamics, or exact acoustic fidelity.

## Windows verification after merging macOS into main (6 October 2026)

Built natively on Windows with Unity 6000.3.25f1, Windows x64, Mono and Direct3D 11, to `Build/Windows/GotlandRing.exe`. The separate macOS ARM64/Metal build entry point and script are preserved; macOS was not rerun on this Windows host.

- Import checks passed against the low-pass CSV, including point count, metric lengths, elevations and malformed input rejection.
- Driving test reached 24.0 m/s, 222.1 m displacement and 5,839 RPM. Braking reached 0.00 m/s (`pass=True`).
- Settings save/cancel, persistence and pause restoration passed. Existing saved steering preferences are preserved; Restore defaults selects the new default response of 2.
- All 42 track signs passed orientation, placement, text-fit and clearance tests (minimum 12.51 m). Swedish lettering and the settings dialog were visually inspected.
- All 820 trees passed clearance checks. No game exceptions, assertion failures or shader errors appeared in the three runtime logs.
- Short rendering sample: 32.0 FPS at 1600x900 on NVIDIA RTX 3090 in this session; not a sustained benchmark.
- Regenerated `PortableLauncher/Game.zip` exclusively from `Build/Windows`, published the .NET 10 self-contained launcher, and verified extraction. The packaged game executable matches the tested executable; track data, both validation images and licensing notices are included.

Logs: `Logs/merge-build-windows.log`, `Logs/merge-smoke-windows.log`, `Logs/merge-settings-windows.log`, `Logs/merge-sign-windows.log` (ignored).
