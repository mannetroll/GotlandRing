# Gotland Ring - Impreza v0.1.4

A detailed Impreza rally car and mapped woodland around the full circuit.

- **High-resolution car:** textured bodywork, cockpit, roll cage, steering wheel and animated road wheels.
- **Single-driver cockpit:** arms, body and belts stay visible; only the driver’s head and helmet are hidden in cockpit view. The co-driver and pace-note book are removed.
- **Paint:** starts in Splash red; **T** cycles red, rally blue and white. **M** remains mute.
- **Camera:** starts behind the car; **C** cycles views. Chase-view mouse movement orbits the car at a fixed radius, including during Auto Pilot. Right mouse returns behind the car.
- **Driving defaults:** steering response is 5. Existing saved settings are retained; use F3 to restore defaults.
- **Lap timing:** **R** recovers the car while preserving lap time and checkpoint progress. **Home** starts a new lap.
- **Mapped forest:** 10,710 pine trees follow the woodland visible in the supplied aerial images. Quarry, paddock and runoff areas remain open, with space around signs and pit garages.
- Includes Auto Pilot, live driving inputs, fullscreen, 16:9 window resizing, the Impreza startup splash and 42 named track boards.

**Windows x64:** download **GotlandRing-Portable.exe** and run it. Unity and .NET are included. The launcher extracts the game into a separate cache for this build.

**macOS Apple Silicon:** download **GotlandRing-macOS-arm64.zip**, extract it, and open **GotlandRing.app**. Requires macOS 12 or later and an M1 or newer Mac. Keep the included data and notices with the app. The app uses a local ad-hoc signature and is not notarized.

The track, tree heights and handling are approximate. Pines use camera-facing cutouts. Scenery collisions and damage are not implemented.

**Car credit:** [Rally Car](https://sketchfab.com/3d-models/rally-car-e0dfd3b6d19947df85002fd8de0a3a02) by SpatialNeglect (@jeandiz), [CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/). Builds containing this model are for noncommercial use. See the included asset credits and data licenses.

**Verified on both platforms:** all five standalone suites passed, including car rendering/animation, driving/braking, settings, all 42 signs and five autopilot laps. The default setup completed its best lap in 3:00.30 with 1.34 m maximum centreline deviation. The Windows game was cross-built on macOS, run on Windows 11, and packaged on Windows. See [verification details](https://github.com/mannetroll/GotlandRing/blob/v0.1.4/VERIFICATION.md).
