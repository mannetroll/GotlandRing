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
In manual driving, W/Up always requests forward throttle without applying brakes,
including while sliding backwards or returning from reverse. It takes priority
over X when both are held; the drive-torque direction changes immediately while
throttle strength ramps up. S/Down applies the brakes. X brakes forward motion
before engaging reverse.

## Cornering slip

The front and rear tyre curves build lateral force progressively to a shared
WheelCollider slip value of 0.20. At large slip (0.60), the front and coasting
rear retain 98% of their peak coefficient. Drive torque reduces the rear's
large-slip coefficient toward 50%, reaching that value at 15% of the wheel's
estimated longitudinal grip budget. The extra loss applies beyond the lateral
peak and uses each wheel's actual drive torque after traction control. Rear
lateral stiffness is 1.20 versus 1.05 at the front. This retains normal
cornering grip while allowing sustained powered reversals to rotate the car
through a sliding turnaround. Releasing W/Up immediately cuts drive and
multiplies all four tyres' longitudinal and lateral grip by **2**, including
while coasting or braking. The rear also regains its coasting curve. During
this grip assist, wheel forces act closer to the centre of mass to limit the
extra body-roll moment. Pressing
W/Up restores normal powered grip on the next physics step; engine torque
builds progressively. The multiplier follows the accelerator request, so gear
changes and traction-control torque cuts while W is held do not trigger it.
The arcade comparison likewise doubles cornering grip and lateral recovery
when its accelerator is released. These
are tuning coefficients, not slip angles in degrees. The [Unity tyre curve](https://docs.unity3d.com/6000.0/Documentation/Manual/wheel-colliders-friction.html)
rises to a peak; grip is bounded even when slip continues to increase. Acceleration and braking demand reduce
the lateral coefficient by up to about 56%, using each wheel's load and torque.
This is a bounded combined-grip approximation, not a calibrated friction-circle
or differential model. The same tyre response applies on the circuit and the training pad. Holding
W while repeatedly reversing the steering can produce a sliding 180° turn; lifting
and countersteering lets the tyres recover. There is no separate drift mode.
The engine, mass and default 50:50 torque split use the stock baseline below.

AWD autopilot follows its direction of travel and allows rotation through bends
with ordinary front-wheel steering. Countersteering strengthens as the slide
grows, with extra recovery above 8°. It retains corner-entry braking up to 6°
of body sideslip and starts reducing throttle at 7°. Its corner-speed plan uses
an 8.8 m/s² grip budget, scaled by the grip setting, while braking and steering
recovery control the slide. The car's rotation and lateral motion
come from tyre forces. The chase camera smoothly follows the direction of travel
so small angles remain visible, and the HUD shows the measured body sideslip.
A nonzero body sideslip angle does not by itself mean a tyre is beyond peak grip;
the telemetry records front/rear tyre slip separately.

## Training area

Press **Z**, or use the area button beneath the mini-map, to switch between
Gotland Ring and an **800 × 800 metre flat asphalt skidpad**. The pad has circles
of 25, 50 and 100 metre radius, a start line and grass beyond the painted edge.
It uses the same tyres, AWD split and steering settings as the circuit. **R** or
**Home** returns to the pad's start; **U** continues to choose mouse or keyboard
steering. `--training` starts a local launch on the pad.

Area switching stops and centres the car, preserves pause, handling/input mode,
autopilot and the circuit best lap, and begins a fresh timed lap at the circuit
start when returning. Training does not record circuit laps. Ctrl/Cmd+P remains
the sole autopilot toggle; on the pad the pilot follows the 100 metre circle.

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
Engine inertia, clutch control, shift time, efficiency, rev limit, torque split, track width,
weight distribution, centre-of-mass height, spring/damper rates, anti-roll rates,
brake torque, tyre curves, drag and rolling resistance are prototype tuning.
There is no detailed centre/rear differential or tyre-temperature model. The
five-speed transmission shifts automatically; the game does not simulate a
manual clutch pedal.

Engine angular velocity is integrated from combustion torque minus engine drag
and the clutch torque transmitted to the wheels, using **0.28 kg·m²** of estimated
rotational inertia. The tachometer and engine audio read this independent RPM
state. Neither vehicle speed nor wheel speed directly assigns engine RPM.
An automatic clutch limits transmitted torque while the engine builds toward
**4500 RPM** at full throttle, allowing the wheels and car to slow during a slide
without immediately dragging the engine down. Above that target, clutch load
couples the engine to the driven wheels through the selected gear. Traction
control's reduced wheel torque also reduces the load reflected to the flywheel.
A fuel limiter progressively reduces combustion torque approaching **6800 RPM**.
Lifting opens the clutch and lets engine drag return the revs to idle; braking
cuts drive. Clutch capacity is 360 Nm and these controls are game tuning, with
no clutch temperature or wear simulation.

Throttle demand raises the downshift threshold from 2400 to 4200 RPM, allowing
the gearbox to skip directly to a suitable lower gear on corner exit. Gear
selection uses the actual driven-wheel RPM rather than assuming those wheels
roll at the car's forward speed. Upshifts normally require 6300 RPM; full throttle
with more than 35% steering demand holds the lower gear to 6700 RPM. Road speed
must also justify the upshift, preventing wheelspin alone from selecting a
higher gear. A downshift leaves 400 RPM below the normal upshift point and
checks for 200 RPM of rev-limit headroom if the tyres regain rolling contact. Each shift
opens the clutch for 0.22 seconds, with at least 0.8 seconds between shift starts.
During that interval, automatic throttle blips match a downshift's shaft speed
before reconnecting. The engine torque curve and gear ratios use the stock
baseline. Vehicle parameters live in `SubaruVehicleSetup.cs`, engine integration
in `SubaruEngine.cs`, and clutch/gear/wheel coordination in `SubaruAwdController.cs`.

The factory brochure lists a 6.3-second 0–100 km/h time for the sedan. That is a
reference, not a claimed measurement of the simulation. The car meshes remain visual proxies;
both selectable models share the stock GT physics.

## Autopilot and validation

AWD autopilot uses the same racing line and ordinary throttle, brake and steering
inputs. Only Ctrl+P (Windows) or Cmd+P (macOS) toggles driving control; WASD,
arrows and X do not override the pilot. Its speed plan uses an AWD lateral acceleration budget and braking
estimate for the physical vehicle. Speed corrections are gradual near the racing line.
The signed pedal demand uses a 0.12-second filter and a small coasting deadband;
braking preview includes that response time. The AWD yaw-error correction uses
a 0.08-second filter to suppress alternating tyre-solver impulses. The path's
steering feed-forward stays immediate, and the cockpit wheel and hands follow
the resulting physical steering. Filter state resets on pilot activation,
handling changes and vehicle recovery.
It does not teleport, align or push the car along the racing line.

Run the macOS executable with `--awd-test -batchmode -nographics` to check training
area switching, pause/reset, both handling modes and manual/autopilot driving;
progressive lateral-force build-up and 24 manual steering recoveries at 60, 100
and 140 km/h; eight full-throttle steering-reversal runs at 100/140 km/h and
eight lift-off recoveries with centred steering or countersteering entirely on flat asphalt; road contact, both banking extremes, the lap seam, all-wheel reverse, braking torque
cut, pause/resume, model/mode switching, 0–100 km/h and braking on the main
straight, and a full physical autopilot lap with power downshifts, modest cornering slip and
clearance checks for the four corners of the car. Tyre-audio checks require quiet
resting, airborne and off-asphalt contacts, plus audible demand in corners.
`GOTLAND_TEST_OUTPUT` optionally sets the results directory; otherwise it uses
`Application.persistentDataPath`. `awd-lap.csv` records speed, target, line error,
road margin, axle torque, contact count, steering, position, body sideslip,
front/rear tyre slip, pedal inputs, gear and RPM at every 0.01-second step.
`AWD_PILOT_SMOOTHNESS` rejects rapid brake reversals above 5% pedal travel and
limits rapid steering reversals above 1.2° at the cockpit rim to fewer than
50 over the full lap. `awd-results.txt`
reports the full-lap result. `awd-grip-curve.csv` measures lateral acceleration
under imposed slip, and `awd-steering-recovery.csv` records full-lock pulses and
35%-lock steering reversals with coasting/full throttle in both directions.
Recovery must remain below 2° residual sideslip and 8°/s yaw rate from 4 seconds
after the manoeuvre begins. `awd-power-slides.csv` records repeated full-throttle
reversals: 0.4-second changes must exceed 12° of body sideslip and 0.8-second
changes must exceed 25°, with the rear tyres beyond peak grip. Separate checks
require releasing W to recover from a 20° slide with both centred steering and
active countersteering. They verify doubled grip, no added drive or brakes,
and restoration of powered grip when W is pressed again. Sustained
full throttle and large reversals can spin the car. Non-batch runs with graphics enabled request
`awd-cornering.png` during a corner and `awd-driving.png` at the end.
`--autopilot-test` validates the separate arcade comparison mode.

`--drivetrain-test -batchmode -nographics` checks free revving with a disconnected
clutch and stationary wheels, the rev limiter, return to idle, loaded revs at
three shaft speeds, and engine continuity when the shaft abruptly stops.
It also drives 20-second circles and steering reversals from 0, 50 and 100 km/h,
with traction control on and off, using the normal W input path. All 12 cases
must keep at least 3000 RPM after the launch transient, avoid drive interruptions
longer than 0.25 seconds, include downshifts, and avoid premature upshifts or
repeated gear changes through the sustained 50 km/h entry turn. `awd-slide-power.csv` records
gear, RPM, speed, sideslip, wheel RPM, steering, drive torque and clutch torque. The same check is included
in `--awd-test`; `GOTLAND_TEST_OUTPUT` sets its output directory.

`--turnaround-test -batchmode -nographics` uses the normal W and A/D input path
on the training pad with default settings, including traction control. It checks
18 combinations: starts at 0/50/100 km/h, A/D changes every 0.4/0.8/1.2 seconds,
and both initial steering directions. Every case must rotate through 180° between headings reached during the
manoeuvre, within 20 seconds, while remaining on asphalt and without applying brakes.
The 0.8/1.2-second reversals must also slide beyond 45° relative to their direction
of travel with the rear tyres past peak grip. `awd-turnaround.csv` records heading,
true slide angle, RPM and tyre slip. These checks also run in `--awd-test`.

The surface's elevation, banking and width estimates retain the limitations in
`track/SURFACE_README.md`. Passing the checks establishes a working prototype;
it does not establish measured real-car fidelity.
