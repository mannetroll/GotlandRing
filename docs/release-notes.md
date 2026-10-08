# Gotland Ring - Impreza v0.2.0

Drive the reconstructed Gotland Ring in either the red Impreza Rally or the blue-and-gold Subaru Impreza. Press **Y** to switch models without interrupting the drive.

- **Banked circuit and minimap:** estimated road widths, banking and crown/hollow profiles follow the supplied 3 m surface data. The minimap uses the matching blue–gray–red banking scale.
- **Racing-line autopilot:** wider entries and exits, smoother corner paths, banking-aware speed planning and advance braking. The controller shares the player's driving physics and rejoins the line after recovery.
- **Impreza audio:** engine-cycle waveforms isolated from the owner's 2022 recording, with cabin speech removed and steady RPM blending to prevent slow volume pulsing.
- **Two animated cars:** steering and rotating road wheels, an animated shared cockpit and driver, and three camera views. **T** cycles the Impreza Rally paint; the Subaru retains its original livery.
- **Mapped scenery:** 12 registry-positioned wind turbines face west and rotate at 10 RPM. The main building sits north of turbine 2; catch fencing, Armco rails and concrete barriers follow the film's trackside styles. The circuit retains 10,710 mapped pine trees and 42 numbered track signs.
- **Native downloads:** self-contained Windows x64 EXE and macOS Apple Silicon ZIP. No Unity installation is required. Download checksums are in `SHA256SUMS`.

![Impreza Rally in Splash red](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.2.0/docs/front.png)

![Subaru Impreza in blue and gold](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.2.0/docs/subaru-impreza.png)

## Verification

All nine standalone suites pass on macOS 15.7.9 with Apple M1 Max/Metal and Windows 11 with RTX 3090/Direct3D 11 on fractal. Each platform completes five racing-line autopilot laps plus recovery; both car models, switching, turbines, scenery, banking, driving/braking, settings and track signs pass. See [verification details](https://github.com/mannetroll/GotlandRing/blob/v0.2.0/VERIFICATION.md) for the measured results and coverage.

## Requirements and scope

Windows 11 x64 with working Direct3D 11 graphics drivers, or macOS 12 or later on Apple Silicon. The Windows EXE is unsigned; the macOS app is ad-hoc signed, without Developer ID signing or notarization.

The track and scenery are approximate reconstructions. Handling remains a simplified ground-following bicycle model without wheel-by-wheel suspension, scenery collisions or damage. The racing line is a practical optimization, not a proven minimum-lap-time solution. The owner's source films are not included in either download.

**Car credits:** [Rally Car](https://sketchfab.com/3d-models/rally-car-e0dfd3b6d19947df85002fd8de0a3a02) by SpatialNeglect and [Subaru Impreza](https://sketchfab.com/3d-models/subaru-impreza-7fb4298d5d8f4185b25bb2c43d7f3787) by Mateusz Woliński, both [CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/). Builds containing these assets are for noncommercial use. Original project source is MIT licensed; geospatial data and third-party assets retain their separate licenses. Attribution accompanies both packages.
