# Gotland Ring — cornering-slip Windows preview

Download **GotlandRing-AWD-Portable.exe** and run it on Windows 11 x64. Unity and .NET are included. This is an unsigned portable preview.

- The AWD tyres allow small, progressive cornering slip in manual driving and autopilot.
- Autopilot follows the direction of travel, counter-steers and eases throttle/braking as the rear steps out.
- The chase camera lets you see the car's angle relative to its motion. The HUD shows actual body sideslip.
- The stock engine/mass baseline and default 50:50 AWD torque split remain in use.

Press **Ctrl+P** for autopilot and **C** to select the chase camera. **F3** opens handling settings; **F4** switches AWD/arcade. **WASD/arrows** drive, **X** reverses, **R** recovers and **Escape** pauses.

The Windows full-lap check covers every track segment in **4:22.15**, with peak sideslip **6.33°** and **0.29 m** minimum clearance for the car's four corners. AWD and arcade checks pass on Windows; repeated arcade runs produce identical lap measurements. All 161 files extracted from the portable build match the tested runtime. The macOS AWD check also passes. These are simulated prototype results, not measured Subaru performance. See [physics assumptions](https://github.com/mannetroll/awd/blob/v0.3.0-awd.2/docs/AWD-PHYSICS.md).

Car assets are for noncommercial use; attribution and track-data notices are included.
