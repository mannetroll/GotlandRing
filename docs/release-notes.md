# Gotland Ring - Impreza v0.1.3

Refreshed 6 October 2026 with the Impreza artwork startup splash, autopilot and desktop-window improvements.

- **Startup splash:** the rebuilt Windows EXE shows the Impreza artwork on black for at least 2.5 seconds during loading. Windows and macOS share the splash configuration.

- **Ctrl+P / Cmd+P:** toggle Auto(P)ilot, with advance braking, corner-speed planning and continuous steering. Any driving key returns control to you.
- **Live WASD display:** green throttle, orange brake and blue steering, with proportional fill.
- **Ctrl+F / Cmd+F:** fullscreen toggle, restoring the previous window dimensions.
- **16:9 window resizing on Windows and macOS**, with correctly scaled HUD and settings.
- Steering response defaults to **2** on both platforms. Existing saved settings are preserved and can be reset through F3.
- Fixed lap checkpoint counting during recovery near the start line.
- Refreshed README, autopilot screenshot and track validation image.
- Retains the shared Windows x64/Direct3D 11 and macOS ARM64/Metal project, 42 named track boards, low-pass CSV elevations, configurable dynamics, reverse and FPS display.

**Windows:** download **GotlandRing-Portable.exe**. No Unity or .NET installation is required. Replace your previous EXE to use this refreshed build.

This refresh replaces the Windows EXE. The macOS ZIP is retained from its previously verified build, before the splash change.

**macOS Apple Silicon:** download [GotlandRing-macOS-arm64.zip](https://github.com/mannetroll/GotlandRing/releases/download/v0.1.3/GotlandRing-macOS-arm64.zip), extract it, then open **GotlandRing.app**. Requires macOS 12 or later and an Apple Silicon Mac (M1 or newer). Unity is included; keep the accompanying track data and notices with the app. The app is not Developer ID signed or notarized; see [Apple's opening instructions](https://support.apple.com/en-us/102445) if macOS blocks it.

**Verification:** five full-track autopilot laps passed on Windows and macOS across multiple handling configurations, plus reverse/off-road recovery. Default best lap: **3:01.12**, top speed **217.9 km/h**, maximum centreline deviation **1.51 m**. Windows resize/maximize/fullscreen checks passed; the extracted portable game scripts match the tested build. macOS driving/braking, settings and all 42 track-sign checks passed. Cmd+P toggled autopilot; Cmd+F entered fullscreen and restored the 1600×900 window while preserving the settings dialog and paused autopilot. See VERIFICATION.md.

The track and handling remain approximate rather than survey/simulator-grade. Attribution and licensing notices accompany the game.

![Auto(P)ilot and live inputs](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.1.3/docs/autopilot-v0.1.3.png)

![Track validation](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.1.3/track/gotland_ring_validation.png)
