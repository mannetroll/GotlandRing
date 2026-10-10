# Gotland Ring — automatic downshifts and persistent autopilot

Download **GotlandRing-AWD-Portable.exe** and run it on Windows 11 x64. Unity and .NET are included. This is an unsigned portable preview.

- **Ctrl+P** toggles autopilot. Driving keys leave it engaged; press Ctrl+P again to drive manually.
- The AWD gearbox downshifts under acceleration to bring the engine into its power band, including on autopilot corner exits. It selects a suitable ratio for the speed instead of forcing third gear everywhere.
- Small cornering slip, counter-steering, the chase camera and sideslip HUD remain active.
- Both car models use the stock engine/mass baseline and default 50:50 AWD torque split.

**C** cycles cameras; **F3** opens handling settings; **F4** switches AWD/arcade. In manual mode, **WASD/arrows** drive and **X** reverses. **R** recovers and **Escape** pauses.

The Windows AWD check completes all **2,405** track segments in **4:19.67**, with
**20 downshifts under power**, **6.75°** peak sideslip and **0.24 m** minimum
clearance for the car’s four corners. AWD and arcade regression checks pass on
Windows and macOS. These are simulated prototype measurements.

See [physics assumptions](https://github.com/mannetroll/awd/blob/v0.3.0-awd.3/docs/AWD-PHYSICS.md). Car assets are for noncommercial use; attribution and track-data notices are included.
