# Gotland Ring - Impreza v0.1.3

Merged the macOS branch into the shared multi-platform main.

- Separate Windows x64 (Direct3D 11) and macOS Apple Silicon ARM64 (Metal) build targets and output folders.
- 42 numbered track-name boards with Swedish names and approach-facing placement.
- Whole-lap low-pass CSV elevation profile, used directly without additional runtime filtering.
- Resizable windows, macOS 16:9 window sizing, refined steering defaults and a brown leather steering wheel.
- Retains FPS/frame-time HUD, configurable dynamics, reverse, curved wing, enclosed cabin and blank plates.

**Windows:** download GotlandRing-Portable.exe. The self-contained executable requires no Unity or .NET installation.

**macOS Apple Silicon:** build from this tag using scripts/Build-macOS.sh with Unity 6000.3.25f1. This release attaches the Windows EXE; a macOS binary is not included.

Windows build, driving/braking, settings persistence, all 42 signs and 820 tree-clearance checks passed after the merge. Portable extraction was verified. macOS build support and its prior verification are preserved; macOS was not rerun on the Windows release host. See VERIFICATION.md.

The track is an approximate geospatial reconstruction. Geometry, terrain and handling are not survey/simulator-grade. Data attribution and notices accompany the game.

![Cockpit](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.1.3/docs/cockpit-hires.png)

![Impreza](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.1.3/docs/rear-v0.1.3.png)
