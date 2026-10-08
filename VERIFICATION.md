# Surface build verification

Verified on 8 October 2026 with Unity 6000.3.25f1 on Apple M1 Max / macOS 15.7.9.

## Builds and data

- macOS ARM64 / Mono / Metal: `Build/macOS/GotlandRing.app` builds successfully and passes `codesign --verify --deep --strict`. This is a local ad-hoc signed build, without Developer ID signing or notarization.
- Windows x64 / Mono / Direct3D 11: `Build/Windows/GotlandRing.exe` cross-compiles successfully with the same source. This surface update has not been runtime-tested on Windows or published as a release.
- The bundled `Surface.csv` matches `track/gotland_ring_full_surface_3m.csv` exactly. Every original centerline column is unchanged from the supplied whole-lap low-pass CSV.
- Import checks pass for 2,405 unique cross-sections, 7,214.397 m horizontal length, asymmetric asphalt boundaries, reconstructed edges, banking sign/magnitude, upward road/apron triangles and matching vertices/normals at the lap seam.
- The road contains 76,960 triangles, with 16 subdivisions across its width. Runtime height queries interpolate these same rendered triangles. Tested polynomial-to-mesh height error is at most 0.00052 m; this is numerical agreement, not source-data accuracy.
- Negative widths, non-finite crossfall and mismatched closing cross-sections are rejected.
- Forest preparation passes with 10,710 trees and at least 19 m canopy clearance from the reference line. Ground heights and sign clearance are regenerated for the new surface.
- Both build outputs include the surface CSV, surface README, validation image, low-pass centerline and attribution notices. Both packaging scripts include the new surface files.

## macOS runtime

- `--surface-test` passes forward/reverse contact, heading preservation, banked recovery, downhill bank-gravity direction and the lap seam. The sampled contact-plane error is at most 0.0119 m. Screenshots at the strongest positive and negative banks and both sides of the seam were captured; both banking directions were visually inspected.
- Modeled edge-to-edge banking ranges from −4.835° to +5.778°. The source remains an estimated reconstruction, as documented in `track/SURFACE_README.md`.
- `--smoke-test` reaches 24.0 m/s, travels 222.7 m from the start after 30 seconds and brakes to 0.00 m/s (`pass=True`). The short rendering sample measured 119.0 FPS at 2912×1638 on Apple M1 Max; this is not a sustained benchmark.
- `--sign-test` passes all 42 boards: right-side placement, approach-facing orientation, text fit and clearance. Six representative driver views were captured.

## Full-lap autopilot

Five laps pass using the ordinary 0.01-second physics step batched between rendered frames. Every scenario visits all 2,405 track segments, brakes, steers in both directions and keeps the car's reference point inside the supplied asphalt boundaries.

| Scenario | Laps | Best simulated lap | Maximum speed | Minimum reference-point margin to asphalt edge |
|---|---:|---:|---:|---:|
| Defaults | 2 | 179.89 s | 217.9 km/h | 2.06 m |
| Low grip / weak brakes | 1 | 246.53 s | 217.7 km/h | 2.43 m |
| High power / slow steering | 1 | 154.44 s | 233.1 km/h | 1.12 m |
| Saved settings | 1 | 185.12 s | 217.9 km/h | 2.08 m |

Pause, toggle and reverse/off-road recovery pass. Recovery finishes 0.24 m from the reference line at 41.46 m/s.

## Evidence and limits

Logs are in the ignored `Logs/` folder: `build-macOS.log`, `build-Windows.log`, `surface-macOS.log`, `surface-smoke-macOS.log`, `surface-signs-macOS.log` and `surface-autopilot-macOS.log`. No game exceptions, shader errors, failed assertions or `pass=False` markers appear in these final logs. Runtime screenshots are saved to the macOS application-support folder recorded in those logs. Reproduction commands are in `README.md`.

The car retains its arcade ground-following bicycle model. Banking affects the cornering allowance and vehicle pose; these checks do not establish surveyed geometry, calibrated tire dynamics, wheel-by-wheel suspension, measured kerbs or accurate broad terrain. Settings UI, car animation and native window resizing were not independently revalidated for this surface change.
