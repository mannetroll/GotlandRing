# Impreza engine audio

The game uses the owner's 2022 in-car recording, `sound/Gotland Ring 2022 - 1 of 1.mp4`, filmed in their Impreza at GotlandRing with Alec Arho-Havrén. The source is a cabin recording, including exhaust, intake and road sound. It is not an isolated engine recording or RPM telemetry.

| Resource | Source time | Estimated reference RPM |
|---|---|---:|
| `ImprezaLow.wav` | 4:47.250–4:49.450 | 2,530 |
| `ImprezaMid.wav` | 2:30.500–2:32.300 | 3,060 |
| `ImprezaHigh.wav` | 3:54.375–3:55.950 | 4,500 |

The game plays isolated engine-cycle waveforms, not the cabin excerpts. Processing tracks slight RPM changes, aligns 46–59 recorded engine cycles and takes their median waveform at each crankshaft phase. This keeps the repeating exhaust shape while rejecting speech and transient cabin sounds. Each asset contains only one 720-degree engine cycle (27–47 ms), so the film's syllables and changing vowel sounds are not replayed. The dominant exhaust pulses are phase-aligned across the three ranges and normalized to the same level. Assets are mono 44.1 kHz PCM.

Playback follows engine RPM, crossfading between the recorded ranges. Throttle changes level and intake brightness; shifts follow the driving model's RPM drop. Lift-off adds a quiet filtered air release. Road noise stays below the engine. Tyre squeal is off by default while its sound is being tuned; the **Tyre squeal** checkbox in **F3** settings enables it for either handling mode. **Apply & close** saves the choice for future launches. This checkbox does not mute engine or road sound. When enabled, squeal becomes prominent as loaded tyres scrub across asphalt. Mute and pause fade to silence without a hard cut. Idle and speeds outside the captured ranges use pitched versions of the nearest loop; they are approximations.

Playback advances by each waveform's exact engine-cycle length, keeping their exhaust pulses aligned even after many laps. Crossfade gain accounts for the shared engine content so overlapping recordings do not become louder. This prevents slow swelling without filtering away the low exhaust note. Road, tyre and lift-off sound is generated separately. The tyre layer combines
band-limited noise with a wavering tone around 1.3–1.6 kHz and a quieter upper
component. Its first tuning uses the owner’s `IMG_0285.mp4` and `IMG_0286.mp4`
as spectral references; it is a synthesis approximation, not an isolated sample.
The original clips are not included in the game.

AWD squeal demand comes from each grounded wheel’s lateral/longitudinal slip
relative to its tyre-curve peaks, weighted by contact load. It builds before
peak grip, then becomes louder as scrub increases. Only asphalt contacts
contribute. Squeal has a 35 ms attack and 160 ms release, fades below driving
speed, and follows the game’s mute/pause control. The arcade comparison uses
its cornering-demand estimate. Neither mode changes physics to produce sound.

To regenerate the assets, install Python 3 with NumPy and ffmpeg, then run:

```bash
python3 scripts/Prepare-EngineAudio.py
python3 -m unittest discover -s scripts -p 'test_engine_audio.py'
```

An alternative source path can be supplied as the first argument. Reproduction needs the owner's original film; normal game builds use the prepared WAV files and do not need the film or Python. The extraction test combines known exhaust pulses with speech-like interference at an independent, changing pitch and syllable envelope, and checks that the exhaust waveform is recovered.

Both Unity build targets run `EngineAudioChecks.Run`. The editor menu **Gotland Ring > Check and preview engine audio** also writes a 24-second idle/acceleration/shift/coast/mute preview and 16-second steady-RPM samples to `Build/AudioPreview/`. Checks cover finite samples, headroom, continuity, silence while muted, stereo channel agreement and 44.1/48/96 kHz output. Twelve steady speeds from 900 to 6,500 RPM must keep the 90th-to-10th percentile spread of their 150 ms RMS envelope below 1.2 dB. The 2,800 RPM check runs after five minutes of playback to catch phase drift. These checks verify signal behavior; they do not replace listening against the original recording.

The builds also produce `impreza-cornering-44100.wav`, `impreza-cornering-48000.wav`
and `impreza-cornering-96000.wav`: 12-second previews that build squeal, release
it, mute and stop. Checks compare the tyre layer with straight driving, require
audible output under slip, and verify stationary silence, headroom and stereo
agreement at all three sample rates. AWD checks cover quiet resting, airborne
and off-asphalt tyres and audible demand through the lap’s corners.
