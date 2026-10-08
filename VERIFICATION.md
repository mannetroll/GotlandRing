# Build verification

Verified on 8 October 2026 with Unity 6000.3.25f1 on Apple M1 Max / macOS 15.7.9 and fractal / Windows 11 Pro build 26300.9457 / NVIDIA GeForce RTX 3090.

## v0.2.0 release checks

Fresh macOS ARM64 and Windows x64 builds succeed from the current release source. All nine standalone suites below pass at 1920×1080 on Apple M1 Max with Metal and on fractal with Direct3D 11 (feature level 11.1). The Windows runs used the signed-in Remote Desktop session and rendered on the RTX 3090; their frame-rate samples are not a local-display performance benchmark. All 159 transferred build-file hashes matched before Windows testing.

| Runtime suite | macOS | Windows 11 |
|---|---|---|
| Car model, cockpit and paint rendering/animation | Pass | Pass |
| Both models, switching state and wheel animation | Pass | Pass |
| Twelve wind turbines, rotor direction/rate and pause | Pass | Pass |
| Main building, barriers, materials and ground clearance | Pass | Pass |
| Banked surface contact, reverse, recovery and lap seam | Pass | Pass |
| Driving and braking | Pass | Pass |
| Settings persistence, cancel/apply and pause restoration | Pass | Pass |
| All 42 track signs | Pass | Pass |
| Five racing-line autopilot laps and recovery | Pass | Pass |

The native builds run track, woodland, turbine, car-import and engine-mixer checks. The Python audio-extraction regression also passes. The macOS executable is ARM64, reports version 0.2.0 and passes `codesign --verify --deep --strict`.

Both README screenshots were freshly captured from the tested macOS build at 1920×1080: `docs/front.png` shows Impreza Rally in Splash red, and `docs/subaru-impreza.png` shows the second model's blue-and-gold livery. Both images were visually inspected.

Current release logs are `Logs/release-*-macOS.log` and `Logs/v0.2.0-Windows-*.log`. Results are collected in `Build/Release-v0.2.0/macOS-tests.json` and `Build/Release-v0.2.0/Windows-evidence/results.json`. The latter folder also contains Windows captures inspected for both car exteriors, the Subaru cockpit, both banking directions, the minimap, turbine rendering, building placement and pit catch fencing. These evidence folders are Git-ignored.

## Main building and roadside barriers

- The hall is placed at approximately X −12.1 m, Z 125.7 m, about 54 m east and 93 m north of mapped turbine 2 (`0980-V-006-003`). Its 38 × 16 m footprint, long-axis orientation and northern annex follow the supplied aerial map. Façade and height are visual approximations documented in `docs/SCENERY.md`.
- Fourteen separate barrier runs reproduce the film's northern pit catch fencing, galvanized Armco rails and southern concrete barriers. The native macOS and Windows `--scenery-test` passes rendered building placement, supported materials, ground contact and asphalt clearance at 2,488 barrier base samples. Minimum sampled barrier-base clearance is 3.48 m; minimum tested building-part clearance is 34.29 m. No mapped trees occupy the building forecourt.
- The building, its relation to turbine 2, and driver views of all three barrier styles were visually inspected. Screenshots are `main-building.png`, `main-building-turbine-2.png`, `pit-catch-fence.png`, `armco-barriers.png` and `concrete-barriers.png` in the game's macOS screenshot folder. Log: `Logs/scenery-macOS.log`.
- Native macOS ARM64 and Windows x64 builds succeed. The Mac app passes signature verification; Windows building and catch-fence captures were visually inspected. Building dimensions and barrier endpoints are reference-based approximations, not survey measurements.

## Wind turbines

- All 12 supplied registry entries are present: six V47 turbines with 55 m hubs / 47 m rotors, three V66s with 78 m hubs / 66 m rotors, and three V90s with 105 m hubs / 90 m rotors. Both builds pass import checks for IDs, counts, dimensions, the shared RH2000 height origin and base/hub/tip relationships. The bundled CSV is byte-for-byte identical to `windmills/gotland_ring_wind_turbines.csv`.
- The macOS and Windows `--wind-test` passes for 12 sites and 36 blades: actual base and hub placement, tower bounds, swept rotor radii and tip heights, rotor speed/direction, fixed hubs/towers, dynamic renderer bounds, static-batch exclusion and pause/resume. Registered positions have no additional offset or terrain snap. Road and apron geometry are unchanged.
- All rotor fronts face west (−X); the scene represents wind arriving from W and flowing east (+X). Rotors turn clockwise from the front at 10 RPM with different initial blade positions. The RPM approximates visible motion around 02:39–02:41 in the owner's `Gotland Ring 2022 - 1 of 1.mp4`; west is a coarse visual choice from the film/layout comparison, with lower confidence than the rotation rate. Yaw, phase, RPM, detailed geometry and ground transitions are scene approximations, not registry measurements.
- V47, V66 and V90 views and the overhead layout were visually inspected in `wind-site-00.png`, `wind-site-06.png`, `wind-site-09.png` and `wind-registry-overview.png` under the application's screenshot folder. Local terrain transitions support the supplied elevations above the approximate landscape. Log: `Logs/wind-macOS.log`.
- macOS ARM64 and Windows x64 builds succeed and include the source CSV, import notes and location map. The macOS app passes signature verification; the Windows runtime passes the same rotor animation checks, and its turbine rendering was visually inspected.

## Banking mini-map

- The mini-map uses the blue–gray–red palette sampled from `track/gotland_ring_surface_validation.png`, with symmetric ±5.8° limits, a degree legend and a white car marker. Positive banking means the right edge is higher. Colors use the edge-to-edge angle, including the effect of asymmetric widths on crowned or hollow sections.
- Both platform builds pass the existing surface checks, including agreement between every mini-map banking value and the CSV within 0.002°. macOS ARM64 and Windows x64 builds succeed.
- macOS screenshots were inspected for color agreement, legend placement and marker visibility. The palette is converted to linear values before IMGUI display. The runtime also passes `--car-switch-test` with both models. Evidence: `Logs/minimap-macOS.log` and `Logs/minimap-preview.png`. The Windows car and banked-surface captures also show the expected minimap colors, legend and marker.

## Car selection

- Both macOS ARM64 and Windows x64 builds include Impreza Rally and the Subaru Impreza derived from the supplied GLB. The Subaru import checks 67 exterior meshes, 4.320 m length, 0.339 m wheel radius, upright orientation, four tires at ground level, front steering pivots and shared cockpit references.
- The macOS and Windows `--car-switch-test` passes switching in all three camera modes with moving and paused state. Position, heading, velocity, RPM, gear, lap/checkpoint progress, best time, autopilot, camera look and audio component remain unchanged. Exactly one model is active, and switching back retains the original car's paint.
- Animation checks pass on both models: forward/reverse wheel roll, steering axes, stable axle centers, pause and cockpit head visibility. Cockpit, bonnet, chase and front exterior screenshots were captured for both cars; the Subaru views were visually inspected.
- Evidence is in `Logs/car-switch-macOS.log`, the build logs and `car-0-*.png` / `car-1-*.png` under the application's macOS screenshot folder. The Windows release log also confirms switching and animation; Windows exterior and Subaru cockpit captures were visually inspected.

## Engine audio

- Native macOS ARM64 and Windows x64 builds succeed with three isolated engine-cycle waveforms derived from the owner's `Gotland Ring 2022 - 1 of 1.mp4` recording. Each contains one 27–47 ms engine cycle extracted across 46–59 recorded cycles; the source conversation is not replayed. The original film is not needed to build or run the game.
- The extraction regression passes at all three reference RPMs with changing, asynchronous speech-like interference. Recovered exhaust waveform correlation is at least 0.998, with relative RMS error below 5.7% against the known clean waveform. This verifies separation on a controlled signal rather than a speech-recognition claim.
- The production mixer passes idle, acceleration, gear-change, coast, mute/unmute and output-device sample-rate checks at 44.1, 48 and 96 kHz. Peak output is at most 0.320, RMS is approximately 0.121 and the largest adjacent-sample step is 0.0131 in the driving preview; no samples clip.
- Twelve 16-second steady-RPM previews from 900 to 6,500 RPM retain the expected exhaust order within 5%. At least 96% of their signal energy is below 400 Hz; less than 0.002% is above 1.6 kHz. This supports the intended deep cabin tone, not a claim of exact acoustic reproduction.
- Slow volume swelling is checked with a 150 ms RMS envelope, requiring its 90th-to-10th percentile spread to remain below 1.2 dB. The worst measured spread is 0.648 dB at idle and 0.531 dB in the driving range. At 2,800 RPM, the spread is 0.367 dB after five minutes of continuous playback. Exact engine-cycle playback prevents drift between the recordings; correlation-aware crossfade gain keeps their overlap at an even volume.
- Previews are generated in `Build/AudioPreview/`; source excerpts and reproduction instructions are in `docs/ENGINE-AUDIO.md`. Windows runtime audio has not been auditioned on a Windows device.

## Builds and data

- macOS ARM64 / Mono / Metal: `Build/macOS/GotlandRing.app` builds successfully and passes `codesign --verify --deep --strict`. This is a local ad-hoc signed build, without Developer ID signing or notarization.
- Windows x64 / Mono / Direct3D 11: `Build/Windows/GotlandRing.exe` cross-compiles successfully with the same source. All nine standalone suites pass on Windows 11 with the RTX 3090.
- The bundled `Surface.csv` matches `track/gotland_ring_full_surface_3m.csv` exactly. Every original centerline column is unchanged from the supplied whole-lap low-pass CSV.
- Import checks pass for 2,405 unique cross-sections, 7,214.397 m horizontal length, asymmetric asphalt boundaries, reconstructed edges, banking sign/magnitude, upward road/apron triangles and matching vertices/normals at the lap seam.
- The road contains 76,960 triangles, with 16 subdivisions across its width. Runtime height queries interpolate these same rendered triangles. Tested polynomial-to-mesh height error is at most 0.00052 m; this is numerical agreement, not source-data accuracy.
- Negative widths, non-finite crossfall and mismatched closing cross-sections are rejected.
- Forest preparation passes with 10,710 trees and at least 19 m canopy clearance from the reference line. Ground heights and sign clearance are regenerated for the new surface.
- Both build outputs include the surface CSV, surface README, validation image, low-pass centerline and attribution notices. Both packaging scripts include the new surface files.

## Driving and surface runtime

- On both platforms, `--surface-test` passes forward/reverse contact, heading preservation, banked recovery, downhill bank-gravity direction and the lap seam. The sampled contact-plane error is at most 0.0119 m. Screenshots at the strongest positive and negative banks and both sides of the seam were captured; both banking directions were visually inspected.
- Modeled edge-to-edge banking ranges from −4.835° to +5.778°. The source remains an estimated reconstruction, as documented in `track/SURFACE_README.md`.
- On both platforms, `--smoke-test` reaches 24.0 m/s, travels 222.7 m from the start after 30 seconds and brakes to 0.00 m/s (`pass=True`). The release smoke-test sample measured 119.8 FPS at 1920×1080 on Apple M1 Max. The Windows sample was 32.0 FPS in the Remote Desktop session; neither sample is a sustained local-display benchmark.
- On both platforms, `--sign-test` passes all 42 boards: right-side placement, approach-facing orientation, text fit and clearance. Six representative driver views were captured.

## Full-lap autopilot

The planned racing line has 28.4% lower integrated squared curvature than the supplied centreline. Its RMS lateral offset is 3.06 m, maximum offset is 7.51 m on the wider side of the asymmetric road, and minimum planned centre-of-car clearance is 2.60 m. The geometry check samples the rendered asphalt at every line point; an overview and three corner details were visually inspected.

Five laps pass on each platform using the ordinary 0.01-second physics step batched between rendered frames. Every scenario visits all 2,405 track segments, brakes, steers in both directions and keeps the car's reference point inside the supplied asphalt boundaries. All four corners of a 4.32 × 2 m car footprint are checked against the rendered asphalt every 0.1 seconds.

| Scenario | Laps per platform | Best simulated lap (macOS / Windows) | Maximum speed | Minimum car-body margin to asphalt edge |
|---|---:|---:|---:|---:|
| Defaults | 2 | 163.10 / 163.10 s | 218.0 km/h | 1.09 m |
| Low grip / weak brakes | 1 | 220.39 / 220.39 s | 232.2 km/h | 1.22 m |
| High power / slow steering | 1 | 141.65 / 141.65 s | 233.1 km/h | 0.75 m |
| Saved settings | 1 | 168.98 / 168.97 s | 218.0 km/h | 1.07 m |

The largest racing-line tracking error after the initial ten seconds is 0.71 m with defaults and 1.00 m across the tested setups. Pause, toggle and reverse/off-road recovery pass. Recovery finishes 0.11 m from the racing line at 33.87 m/s on macOS and 33.83 m/s on Windows. The planner optimizes geometric smoothness with grip, banking, braking and steering constraints; these tests do not establish a global minimum lap time.

## Release packages

- `GotlandRing-macOS-arm64.zip` passes archive integrity checks. Its extracted app is byte-for-byte identical to the tested app and passes `codesign --verify --deep --strict`; the bundled README matches the release source. The copy installed at `/Applications/GotlandRing.app` also matches and passes signature verification.
- `GotlandRing-Portable.exe` was built on fractal with .NET SDK 10.0.401. Launcher and game versions are both 0.2.0. `--extract-only` succeeds, and all 160 extracted payload files match the package inputs by SHA-256. Launching the final portable EXE in the signed-in Windows session also passes the driving/braking smoke test on the RTX 3090.
- Both downloads include the attribution and track/turbine data; the owner's source films are excluded. `SHA256SUMS` accompanies the release assets. Package evidence is in `Build/Release-v0.2.0/Windows-package.json` and `Logs/v0.2.0-portable-smoke.log`.

## Evidence and limits

Build logs and all release-suite logs are in the ignored `Logs/` folder. No game exceptions, shader errors, failed assertions or `pass=False` markers appear in the successful release logs. Runtime screenshots and `racing-line.csv` are saved to the macOS application-support folder or beside the Windows game, as recorded in those logs. Retrieved Windows evidence is in `Build/Release-v0.2.0/Windows-evidence/`; the inspected racing-line map is in `Build/RacingLine/`. Reproduction commands are in `README.md`.

The car retains its arcade ground-following bicycle model. Banking affects the cornering allowance and vehicle pose; these checks do not establish surveyed geometry, calibrated tire dynamics, wheel-by-wheel suspension, measured kerbs or accurate broad terrain. Settings persistence, pause restoration and car animation are covered by the release suites. Manual native window resizing/fullscreen transitions and Windows audio playback were not independently revalidated in this release run.
