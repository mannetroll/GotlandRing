# Gotland Ring — Subaru AWD Windows preview

Download **GotlandRing-AWD-Portable.exe** and run it on Windows 11 x64. Unity and .NET are bundled; no separate installation is required. The executable is unsigned. The first launch extracts the game into the current user's local application-data folder.

The stock GT baseline uses a Rigidbody and four WheelColliders for suspension, tyre contact and all-wheel drive on the banked circuit. The five-speed manual gearbox ratios shift automatically for keyboard play. Tyre, suspension, gearing and differential details remain provisional; this is a playable prototype, not a calibrated Subaru simulator.

- **WASD / arrows:** accelerate, brake and steer; **X:** reverse.
- **F4:** switch AWD / arcade handling and restart the lap.
- **F3:** adjust grip, torque split, steering speed and traction control.
- **Ctrl+P:** toggle autopilot; **Escape:** pause / resume.
- **C:** change camera; **Y:** switch car model; **R:** recover to the circuit.

See [the physics baseline and limitations](https://github.com/mannetroll/awd/blob/v0.3.0-awd.1/docs/AWD-PHYSICS.md). Car models are licensed for noncommercial use; attribution and track-data notices are included in the package.

Native Windows checks on `fractal` pass for AWD contact, banking, reverse, braking, pause, car/mode switching and a complete lap, plus the arcade five-lap and recovery regression. The AWD lap is **4:27.34**, simulated 0–100 km/h is **7.26 s**, and 100–0 km/h stopping distance is **35.83 m**. These are prototype measurements, not real-car performance claims.

The portable package passes extraction checks for all 161 files and completes the AWD suite with Direct3D 11 graphics in the Windows desktop session.
