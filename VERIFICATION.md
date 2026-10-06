# Verification

Build and runtime checks use an Apple M1 Max running macOS 15.7.9 with Unity 6000.3.25f1.

Current source coverage: the macOS build succeeds and is installed at `/Applications/GotlandRing.app`. Car import, rendering, animation and paint cycling have been checked. The orbit camera, steering response 5, red/rear-view defaults and lap-preserving recovery have been compiled without further runtime tests. Windows cross-compilation covers the car and paint changes; its current source and packaged launcher still need rebuilding. Full-lap results below use steering response 2.

## Builds

- macOS: `Build/macOS/GotlandRing.app`, native ARM64, Mono, Metal. The app and embedded libraries pass `codesign --verify --deep --strict` with Unity's local ad-hoc signature; this is not a notarized distribution.
- macOS release ZIP: needs repackaging from the current app before publishing. The installed app has the current source changes.
- Windows: `Build/Windows/GotlandRing.exe`, x86-64, Mono, Direct3D 11. Cross-compilation with the imported rally car succeeded on macOS (`Logs/rally-build-Windows.log`). The Windows executable was not run in this verification, and the committed portable launcher payload was not regenerated.
- Both builds pass `TrackImportChecks`: the bundled CSV matches `track/gotland_ring_full_centerline_3m_lowpass.csv`, with 2,405 unique points, approximately 7,214.398 m horizontal length / 7,216.638 m 3D length, minimum/maximum Y of −19.445 / 3.929 m, all-point height projection, and malformed input rejection. Unity's single-precision length accumulation reports 7,216.641 m in 3D.
- Both output folders include the low-pass source centerline, validation image, low-pass elevation profile, track documentation, and license/asset notices. The bundled resource and distributed CSVs match the source byte for byte. Windows packaging reads only `Build/Windows` and includes both track images.

## macOS runtime

- The SpatialNeglect rally car imports with 91 meshes and 19 assigned materials. The prefab is 4.32 m long, grounded at its tyre bottoms, with a 0.339 m wheel radius. Both build targets prepare the same prefab.
- `--model-preview`: forward/reverse wheel rotation, front steering axes, stable axle centres, paused animation, cockpit driver visibility and an unchanged physics root all pass. Cockpit forward/left/right, bonnet, chase, front, rear and steered-wheel captures were visually inspected. Evidence: `Build/RallyCar/` and `Logs/rally-model-macOS.log` (ignored).
- With the imported car, driving/braking, five full autopilot laps, all 42 track signs, and settings save/cancel/persistence/pause checks passed. Logs: `Logs/rally-smoke-macOS.log`, `Logs/rally-autopilot-macOS.log`, `Logs/rally-sign-macOS.log`, `Logs/rally-settings-macOS.log`. The installed app also passed the driver-view sign check (`Logs/rally-installed-sign-macOS.log`). No game exceptions or shader errors were reported.
- The custom Impreza startup splash was visually checked in the rebuilt macOS app: centered artwork on black, no Unity logo, followed by the driving scene. The release build's standalone smoke test reached 24.0 m/s and braking reached 0.00 m/s (`pass=True`); log: `Logs/release-macOS-smoke.log`.
- Native macOS window resizing preserves the game area's 16:9 aspect ratio, excluding the title bar. Corner, side and bottom-edge drags were checked at approximately 1428×804, 1202×676 and 1596×898 (AppKit rounds to whole display points). The HUD, pause menu and driving settings retain their proportions; F3 and Escape were checked at the resized dimensions.
- `--smoke-test` with the imported car: reached 24.0 m/s (86.4 km/h), 221.7 m displacement from the start, and 5,822 RPM after 30 seconds. Four seconds of braking reduced speed to 0.00 m/s (`pass=True`).
- The runtime reads the low-pass CSV coordinates directly, with no additional height filtering. Imported elevation range is 23.374 m.
- Rendering sample with the imported car and all 42 track signs: 118.0 FPS at 2866×1612 using the Apple M1 Max Metal device. This is a short sample, not a sustained performance benchmark.
- Runtime tree-clearance check passed for 820 trees. No game exceptions or shader errors were reported in the driving test.
- The captured cockpit screenshot was inspected. macOS screenshots are written to `Application.persistentDataPath`, outside the signed app bundle.
- `--sign-test`: all 42 numbered name boards pass right-side placement, approach-facing orientation, text-fit and road/runoff-clearance checks. Minimum clearance from any track segment is 12.51 m, beyond the 11.5 m runoff boundary. Driver-view screenshots of signs 1, 3, 11, 31, 35 and 42 were inspected, including paired names and Swedish characters. Lettering respects scenery and car-body depth.
- The panoramic `Sky.hdr` texture is imported without mipmaps to prevent a vertical filtering seam at the longitude wrap. The sky is continuous in the six captured driver views; the corresponding render check is logged in `Logs/sky-macOS.log`.

Build and runtime logs are in the ignored `Logs/` folder. Commands to reproduce these checks are in `README.md`. Full-lap autopilot checks are detailed below; these checks do not validate calibrated vehicle dynamics or exact acoustic fidelity.

## Windows verification after merging macOS into main (6 October 2026)

Built natively on Windows with Unity 6000.3.25f1, Windows x64, Mono and Direct3D 11, to `Build/Windows/GotlandRing.exe`. The separate macOS ARM64/Metal build entry point and script are preserved; macOS was not rerun on this Windows host.

- Import checks passed against the low-pass CSV, including point count, metric lengths, elevations and malformed input rejection.
- Driving test reached 24.0 m/s, 222.1 m displacement and 5,839 RPM. Braking reached 0.00 m/s (`pass=True`).
- Settings save/cancel, persistence and pause restoration passed. Existing saved steering preferences are preserved.
- All 42 track signs passed orientation, placement, text-fit and clearance tests (minimum 12.51 m). Swedish lettering and the settings dialog were visually inspected.
- All 820 trees passed clearance checks. No game exceptions, assertion failures or shader errors appeared in the three runtime logs.
- Short rendering sample: 32.0 FPS at 1600x900 on NVIDIA RTX 3090 in this session; not a sustained benchmark.
- Regenerated `PortableLauncher/Game.zip` exclusively from `Build/Windows`, published the .NET 10 self-contained launcher, and verified extraction. The packaged game executable matches the tested executable; track data, both validation images and licensing notices are included.

Logs: `Logs/merge-build-windows.log`, `Logs/merge-smoke-windows.log`, `Logs/merge-settings-windows.log`, `Logs/merge-sign-windows.log` (ignored).

## Autopilot and fullscreen (6 October 2026)

- Windows standalone full-lap regression uses the game's ordinary 0.01-second physics step, batched between frames. Five laps passed, with all 2,405 track segments visited in each scenario. These timings are simulated driving times, not test wall-clock durations.
- Default handling: two laps, best 181.12 seconds, maximum 217.9 km/h, maximum centreline deviation 1.51 m.
- macOS ARM64/Metal passed all five laps with all 2,405 track segments visited per scenario. Default, low-grip/weak-brake and high-power/slow-steering results match Windows; the Mac's saved-settings lap took 187.73 seconds. Reverse/off-road recovery finished 0.08 m from the centreline at 34.66 m/s. Driving/braking, settings and all 42 track-sign checks also passed.
- Low grip / weak brakes: 248.28 seconds, maximum 217.0 km/h, maximum deviation 0.94 m. High power / slow steering: 154.68 seconds, 233.1 km/h, maximum deviation 2.28 m. The saved-settings lap also passed (185.50 seconds).
- Every scenario exercised braking and both steering directions. Reverse/off-road recovery, pause preservation and autopilot state checks passed. Recovery now requires actual forward checkpoint crossings before a lap can count.
- The live WASD overlay was inspected in both players. Windows Ctrl+P and macOS Cmd+P toggled autopilot; paused inputs were also inspected on macOS.
- Fullscreen Windows build passed. Actual Ctrl+F input switched from a 1600x900 window to 1920x1200 borderless fullscreen and back to exactly 1600x900. Both transitions were visually inspected and confirmed by `DISPLAY_MODE` log entries. No game exception was logged.
- macOS Cmd+F entered fullscreen and returned to the 1600×900 game window. Both transitions were visually inspected, with the settings dialog and paused autopilot preserved. No runtime exception was logged.

Logs: `Logs/autopilot-laps.log`, `Logs/fullscreen-build.log`, `Logs/fullscreen-controls.log`, `Logs/autopilot-macOS.log`, `Logs/controls-macOS.log`, `Logs/smoke-macOS.log`, `Logs/settings-macOS.log`, `Logs/sign-macOS.log` (ignored).

### Windows 16:9 resize recheck

Native Windows resizing was exercised with the settings dialog open. Corner, right-edge and bottom-edge drags produced client areas of 1259x708, 1099x618 and 940x529 respectively, all within half-pixel aspect rounding. The dialog and HUD remained correctly proportioned. Maximizing produced a 1920x1080 game area beneath the title bar. Ctrl+F entered 1920x1200 borderless fullscreen and restored exactly 940x529 on return, with the settings dialog and paused state preserved. No runtime exceptions were logged. Evidence: `Logs/aspect-recheck.log`; native window screenshots were visually inspected.

Release refresh: the final Windows build passed all five autopilot laps again. The saved-settings lap with steering response 2 completed in 186.40 seconds; reverse/off-road recovery ended 0.37 m from the centreline. Fresh 1600x900 screenshot: docs/autopilot-v0.1.3.png. Logs: Logs/release-autopilot-build.log and Logs/release-autopilot-test.log.

## Windows splash release refresh (6 October 2026)

Rebuilt Windows x64 from the shared Impreza artwork splash configuration. Unity's splash preparation and build succeeded. Standalone driving/braking passed: 24.0 m/s, 222.2 m displacement, then 0.00 m/s under braking. All 820 trees passed clearance validation; no game exceptions were logged. Logs: Logs/splash-release-build.log and Logs/splash-release-smoke.log.
