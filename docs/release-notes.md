# Gotland Ring - Impreza v0.1.3

Refreshed 6 October 2026 with autopilot and desktop-window improvements.

- **Ctrl+P / Cmd+P:** toggle Auto(P)ilot, with advance braking, corner-speed planning and continuous steering. Any driving key returns control to you.
- **Live WASD display:** green throttle, orange brake and blue steering, with proportional fill.
- **Ctrl+F / Cmd+F:** fullscreen toggle, restoring the previous window dimensions.
- **16:9 window resizing on Windows and macOS**, with correctly scaled HUD and settings.
- Steering response defaults to **2** on both platforms. Existing saved settings are preserved and can be reset through F3.
- Fixed lap checkpoint counting during recovery near the start line.
- Refreshed README, autopilot screenshot and track validation image.
- Retains the shared Windows x64/Direct3D 11 and macOS ARM64/Metal project, 42 named track boards, low-pass CSV elevations, configurable dynamics, reverse and FPS display.

**Windows:** download **GotlandRing-Portable.exe**. No Unity or .NET installation is required. Replace your previous EXE to use this refreshed build.

**macOS Apple Silicon:** build this tag with `scripts/Build-macOS.sh` and Unity 6000.3.25f1. No macOS binary is attached. The new shortcuts still need runtime verification on a Mac.

**Verification:** five full-track autopilot laps passed across multiple handling configurations, plus reverse/off-road recovery. Default best lap: **3:01.12**, top speed **217.9 km/h**, maximum centreline deviation **1.51 m**. Windows resize/maximize/fullscreen checks passed; the extracted portable game scripts match the tested build. See VERIFICATION.md.

The track and handling remain approximate rather than survey/simulator-grade. Attribution and licensing notices accompany the game.

![Auto(P)ilot and live inputs](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.1.3/docs/autopilot-v0.1.3.png)

![Track validation](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.1.3/track/gotland_ring_validation.png)
