# Subaru AWD driving mode

The AWD mode uses a Unity Rigidbody and four WheelColliders on the rendered
Gotland Ring road, aprons and shoulders. Gravity, tyre forces, spring/damper
forces and anti-roll forces move the car. The car is placed on the track when
recovering; normal driving does not snap its height or heading to the surface.
The broad surrounding terrain also has a collider. Decorative scenery and
barriers are not a complete collision environment.

Open **Dynamics (F3)** to select **AWD physics** or **Arcade comparison**.
Press **F4** outside the settings panel to switch modes directly. Changing mode restarts the lap and clears its best time, so times from the two
models are not mixed. AWD is the default for a new setup. `--awd` and `--arcade`
select a mode for one launch. Existing keyboard controls, cameras, model/paint
switching and Cmd+P autopilot work in both modes. **U** toggles horizontal mouse
steering; the keyboard retains the accelerator, brake and reverse controls.
AWD and Arcade share the same low- and high-speed steering settings, defaulting
to **42°** and **19°**. Both F3 panels expose the same **25–55°** low-speed range
and **8–25°** high-speed range. The maximum wheel angle interpolates linearly
from rest to **65 m/s (234 km/h)** and stays at the high-speed value above that.
With the defaults, full input gives approximately **32.2° at 100 km/h**.
Keyboard, mouse, physical wheel steering and the visible wheels use this range.
Autopilot plans and normalizes its steering demand against the same settings.
The AWD tyre forces still determine how much the car actually turns.

The AWD settings also expose tyre grip, front torque share, steering speed and
traction-control assistance. The default split sends 50% of one engine-torque
budget to each axle, then divides that equally between the axle's wheels.
Reverse uses the same distribution. Each wheel has its own slip-based traction
control and ABS; braking cuts engine drive. Traction control is a gameplay
assist, not a claim about factory equipment on the 1999/2000 car.

## Cornering slip

The front and rear tyre curves build lateral force gradually and retain 90% of
their peak coefficient at large slip. Acceleration and braking demand reduce
the lateral coefficient by up to about 13%, using each wheel's load and torque.
This is a bounded combined-grip approximation, not a calibrated friction-circle
or differential model. The same tyre response applies to manual driving.
The engine, mass and default 50:50 torque split use the stock baseline below.

AWD autopilot follows its direction of travel and allows rotation through bends
with ordinary front-wheel steering. Countersteering strengthens as the slide
grows, with extra recovery above 8°. It retains corner-entry braking up to 6°
of body sideslip and starts reducing throttle at 7°. Its corner-speed plan uses
a 9.2 m/s² grip budget, scaled by the grip setting, while braking and steering
recovery control the slide. The car's rotation and lateral motion
come from tyre forces. The chase camera smoothly follows the direction of travel
so small angles remain visible, and the HUD shows the measured body sideslip.
A nonzero body sideslip angle does not by itself mean a tyre is beyond peak grip;
the telemetry records front/rear tyre slip separately.

## Stock/manual baseline

The reference is Subaru Germany's factory [1999/2000 Impreza brochure](https://www.subaru.de/hubfs/Service%20und%20Zubeh%C3%B6r/Prospektarchiv/Impreza/Impreza_MJ1999-2000_PTA.pdf?hsLang=de),
technical-data pages 4–5, **2.0 GT Turbo sedan, five-speed**. It is a suitable
stock reference, not a verified identification of the user's exact model year
or market specification.

| Parameter | Simulation baseline | Basis |
| --- | --- | --- |
| Engine output | 160 kW at 5600 RPM | Factory brochure |
| Peak torque | 290 Nm at 4000 RPM | Factory brochure |
| Kerb mass | 1285 kg | Sedan column, without driver |
| Simulated mass | 1360 kg | Kerb mass plus an assumed 75 kg driver |
| Wheelbase | 2.52 m | Factory dimension drawing |
| Tyres | 205/50 R16; nominal radius 0.3057 m | Factory size; radius calculated without loaded deflection |
| Transmission | Five forward gears | Factory brochure; automatic shifting for keyboard play |
| Forward ratios | 3.454 / 1.947 / 1.366 / 0.972 / 0.738 | Provisional five-speed ratios; exact gearbox code unverified |
| Final drive / reverse | 4.111 / 3.333 | Provisional; exact gearbox code unverified |

The torque curve is estimated between the two published engine anchors.
Launch-clutch slip, shift time, efficiency, rev limit, torque split, track width,
weight distribution, centre-of-mass height, spring/damper rates, anti-roll rates,
brake torque, tyre curves, drag and rolling resistance are prototype tuning.
There is no detailed centre/rear differential or tyre-temperature model. The
five-speed transmission shifts automatically; the game does not simulate a
manual clutch pedal. Throttle demand raises its downshift threshold from 2400 to
4200 RPM, allowing it to skip directly to a suitable lower gear on corner exit.
Downshifts leave 400 RPM below the upshift point to avoid hunting, and each shift
cuts drive for 0.22 seconds. The engine output and gear ratios stay at the stock
baseline. Vehicle parameters live in `SubaruVehicleSetup.cs`; automatic gear
selection lives in `SubaruAwdController.cs`.

The factory brochure lists a 6.3-second 0–100 km/h time for the sedan. That is a
reference, not a claimed measurement of the simulation. No GRID BMW parameters
are used by this integrated controller. The car meshes remain visual proxies;
both selectable models share the stock GT physics.

## Autopilot and validation

AWD autopilot uses the same racing line and ordinary throttle, brake and steering
inputs. Only Ctrl+P (Windows) or Cmd+P (macOS) toggles driving control; WASD,
arrows and X do not override the pilot. Its speed plan uses an AWD lateral acceleration budget and braking
estimate for the physical vehicle. Speed corrections are gradual near the racing line.
It does not teleport, align or push the car along the racing line.

Run the macOS executable with `--awd-test -batchmode -nographics` to check road
contact, both banking extremes, the lap seam, all-wheel reverse, braking torque
cut, pause/resume, model/mode switching, 0–100 km/h and braking on the main
straight, and a full physical autopilot lap with power downshifts, modest cornering slip and
clearance checks for the four corners of the car. Tyre-audio checks require quiet
resting, airborne and off-asphalt contacts, plus audible demand in corners.
`GOTLAND_TEST_OUTPUT` optionally sets the results directory; otherwise it uses
`Application.persistentDataPath`. `awd-lap.csv` records speed, target, line error,
road margin, axle torque, contact count, steering, position, body sideslip,
front/rear tyre slip, pedal inputs, gear, RPM and normalized tyre-squeal demand. `awd-results.txt`
reports the full-lap result. Non-batch runs with graphics enabled request
`awd-cornering.png` during a corner and `awd-driving.png` at the end.
`--autopilot-test` validates the separate arcade comparison mode.

The surface's elevation, banking and width estimates retain the limitations in
`track/SURFACE_README.md`. Passing the checks establishes a working prototype;
it does not establish measured real-car fidelity.
