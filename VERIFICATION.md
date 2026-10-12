# Gotland Ring v0.3.3 verification

Verified on 12 October 2026 with Unity 6000.3.25f1, an Apple M1 Max Mac and Windows 11 on Fractal over SSH. The ARM64 macOS and x64 Windows packages use the same release source and game version **v0.3.3**. Windows uses Direct3D 11; macOS uses Metal.

## Standalone checks

All **14 macOS suites** pass: scenery views, cockpit instruments, feedback, AWD, arcade autopilot, settings, reference lap, scenery geometry, surface contact, model animation, car switching, wind turbines, track signs and driving/braking smoke checks. Rendering checks and screenshots run with Metal enabled.

All **eight Windows functional suites** pass: scenery views, cockpit instruments, feedback, car switching and driver animation, AWD, arcade autopilot, settings and reference lap. Windows runs use `-batchmode -nographics`, validating simulation, skinned-mesh deformation, camera state, replay, timing and controls without testing rendered appearance or sound output. The instrument suite explicitly skips GPU checks in this mode.

| Measurement | macOS | Windows 11 |
|---|---:|---:|
| AWD circuit autopilot lap | 262.41 s | 262.41 s |
| Track segments visited | 2,405 / 2,405 | 2,405 / 2,405 |
| Maximum speed | 177.0 km/h | 177.0 km/h |
| Minimum car-body clearance to asphalt edge | 0.51 m | 0.51 m |
| Maximum lap body sideslip above 43 km/h | 4.44° | 4.44° |
| Time above 3° sideslip in corners | 41.66 s | 41.65 s |
| Downshifts under power | 10 | 10 |
| Main-straight 0–100 km/h | 7.52 s | 7.52 s |
| Braking distance from approximately 100 km/h | 35.72 m | 35.95 m |
| Lowest sustained RPM in held-throttle slide checks | 4,323 | 4,323 |
| Longest interruption of drive torque in those checks | 0.23 s | 0.23 s |
| Rapid brake pulses during the lap | 0 | 0 |
| Rapid steering reversals during the lap | 6 | 16 |

## Engine RPM, sliding and autopilot

Engine checks cover free revving with an open clutch, the limiter, return to idle, load at stationary wheels and sudden shaft deceleration. Twelve W-held circle/reversal runs start at 0/50/100 km/h, with traction control on and off. They require sustained RPM and drive torque, power downshifts without gear hunting and no unrequested wheel braking.

All 18 keyboard turnaround cases reach 180°. The 0.8/1.2-second steering reversals also require at least 45° of sideslip. The 24 manual steering recovery cases, eight full-throttle reversal runs and eight lift-off recoveries pass on both platforms. Releasing W applies 2× grip immediately; both centred-steering and countersteering recoveries catch the slide, and pressing W restores powered sliding.

The lap test records controls at 100 Hz. Rapid brake pulses count adjacent rise/fall changes greater than 0.05; steering reversals count alternating road-wheel changes greater than 0.1° per step (approximately 1.2° at the steering-wheel rim). Both platforms pass the smoothness limits. The filters apply to autopilot pedal demand and yaw correction.

Accelerator checks also cover 20 spins beyond 90° across both surfaces and both handling modes. W/Up retains forward throttle with no automatic wheel braking. Returning from reverse releases reverse torque immediately and gives W priority over an overlapping X input. S/Down still brakes and cuts drive. Circuit and training-pad physics remain shared. The arcade regression covers five full laps and off-road/reverse recovery.

Start/finish checks require ordered checkpoints and a forward finish crossing. Reverse crossings, rocking across the line and recovery do not add laps.

## Scenery tour

`--scenery-views-test` checks all 15 views used by **V**, including camera clearance above the terrain, orthographic overview and early exit. Eight restoration combinations cover AWD/arcade, circuit/training and paused/running state, using both car models. The saved car position, orientation, momentum, RPM, gear, autopilot, lap/checkpoints, elapsed lap time and driving-camera projection remain intact. Mac captures cover every view. C or Escape returns early; advancing beyond the last view returns to the car.

## Cockpit and surface feedback

The live LCD passes driving-state checks for AWD and arcade, both models, sideways speed, reverse, pause and model isolation on both platforms. Mac GPU readback verifies rev-bar fill and changing speed/RPM digits. Cockpit screenshots show 108 km/h, 4500 RPM and third gear matching the HUD, with the v0.3.3 label. Replay checks verify instrument readings after seeking, pausing and finishing.

Steering checks cover the visible wheel, its centred column axis, road-wheel agreement, both directions, full locks, pause and return to centre. Both car models pass the driver checks: gloves follow their bones, elbows remain connected, the torso stays fixed, grip changes alternate, and pause/seek/centre poses are stable. Sweeping the wheel from −720° to +720° in 0.5° steps gives a maximum hand displacement of 0.0095 m per step and at least 0.070 m of arm reach remaining.

Feedback checks cover grounded, split-surface and airborne wheel contacts, stationary settling, acceleration, disabled sliders, mouse look, chase-camera isolation and recovery. Editor checks validate 711 kerb sample locations, asphalt/loose-ground and training-pad boundaries, braking/cornering direction, 30/60/120 Hz behaviour and motion limits.

Engine and surface-audio checks pass at 44.1, 48 and 96 kHz, including level/headroom, mute, zero intensity, stationary/airborne settling and sample-rate changes. Settings checks restore saved preferences after apply/cancel and movement/sound persistence checks.

## Replay and scenery geometry

The reference-lap suite validates all 4,464 motion/audio samples, circuit coverage, clock boundaries, pause/mute/completion, eight drive-restoration combinations, momentum, lap-time isolation, camera proportions and audio-listener cleanup. Feedback settings and steering animation also work in replay.

Scenery checks validate all **26 kerb runs / 13,812 vertices**, road sides, upward faces and a 0.025 m paint offset. Quarry mesh and driving-surface queries agree with collision geometry. Raised banks leave the road and shoulder clear. The mapped main building, 14 barrier runs and all 12 west-facing turbines pass placement and geometry checks. Surface checks cover forward/reverse contact, banking, recovery and the seam, with a maximum contact error of 0.0119 m.

## Build and package checks

Both Unity builds pass track/surface, turbine, forest, car-import, ride-feedback and engine/surface-audio checks.

The macOS ZIP passes archive-integrity checking. All **147 app files** and **18 accompanying documentation/data files** match their sources. The extracted app reports v0.3.3, passes `codesign --verify --deep --strict`, and passes the headless instrument startup check.

The Windows portable EXE is built with .NET 10. The launcher metadata and serialized Unity game settings report v0.3.3; the Unity executable's own file metadata reports the Unity engine version. All **161 transferred runtime files** and **161 extracted payload files** match by SHA-256. The final portable EXE starts the game and reaches the settings-test success marker. Its retrieved checksum matches the Windows packaging result.

The final Mac app is installed in `/Applications/GotlandRing.app`; all 147 files match the tested build. Its 1344 × 784-point startup window is centred in the 1684 × 1079-point usable display area and fits within 80%. Interactive checks confirm V opens and advances scenery views, and C restores the driving camera.

The README screenshots were captured from this release at 1920×1080: the red Impreza Rally, blue-and-gold Subaru Impreza and live cockpit instruments. Both downloads include attribution and track/turbine data. Source films remain excluded. `SHA256SUMS` records the final package hashes.

## Reproduction and evidence

Run the standalone checks with the flags in [README.md](README.md). Core simulation suites support `-batchmode -nographics`; scenery material checks need graphics enabled. Each suite must exit successfully and report its success marker without failed assertions. See [AWD assumptions and controls](docs/AWD-PHYSICS.md).

Local evidence is outside Git in `Logs/release-0.3.3/` and `Build/Release-v0.3.3/`. Build logs are `Logs/build-macOS.log` and `Logs/build-windows-0.3.3.log`.

## Limits

Windows visual appearance, native window resizing/fullscreen and audio listening were not reviewed in the SSH session. Audio checks validate generated samples; they do not replace listening feedback.

These checks validate prototype behaviour, not calibrated Subaru dynamics or surveyed circuit geometry. The reference lap is a position-matched replay with estimated audio controls, not measured Subaru telemetry. Scenery collisions, damage, detailed differentials and tyre-temperature simulation remain incomplete or absent. Packages have no Windows publisher signature or Apple Developer ID/notarization; the Mac app is ad-hoc signed. Car-asset use remains noncommercial under the included notices.
