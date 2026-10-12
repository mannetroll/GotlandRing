# Make a Gotland Ring trailer

These sources reproduce the **20-second v0.3.2 trailer**, captured locally on an
Apple Silicon Mac: 1920×1080, 30 fps, H.264 video and stereo AAC audio in a
fast-start MP4. The finished file is `docs/GotlandRing-Trailer.mp4`.

The capture uses the game's AWD simulation, records motion at 100 Hz, then
replays it through five cameras. Wheel suspension, steering, driver animation,
cockpit instruments and wind turbines remain animated. Engine and surface
audio come from the game's mixers, synchronized to the recorded driving state;
the drive-by adds distance attenuation and Doppler. There is no film dialogue
or added music.

| Time | Shot |
|---|---|
| 0–3 s | Red Impreza tracking shot on the circuit |
| 3–8 s | Cockpit driving through Senna S, with live instruments |
| 8–13 s | Blue-and-gold Impreza doing AWD slides on the training pad |
| 13–18 s | Low trackside drive-by on Gutemålrakan, about 141 km/h at the camera |
| 18–20 s | Blue-and-gold Impreza tracking shot, title and credits |

## Requirements

- This repository, including its prepared Unity scene, car prefabs and audio.
- macOS on Apple Silicon, with a logged-in graphical desktop for capture.
- Licensed Unity **6000.3.25f1**, matching `ProjectSettings/ProjectVersion.txt`.
  See [macOS setup](../Unity_macOS_Setup.md) for installation.
- Swift and AppKit, available through Xcode or the Xcode Command Line Tools.
- `ffmpeg` and `ffprobe` on `PATH` (for example, `brew install ffmpeg`).

Close the Unity editor for this project before building. Run the following
commands in a terminal from the repository root. No Windows machine or SSH
connection is required.

## 1. Build the capture app

Unity compiles scripts inside `Assets/`, so the build temporarily copies the
two C# files and their `.meta` files there. This subshell removes those copies
when it exits, including if the build fails. Keep the maintained source here
in `Trailer/`.

```bash
(
  set -euo pipefail
  unity_version="$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)"
  unity_editor="${UNITY_EDITOR:-$PWD/Tools/Unity/$unity_version/Unity.app/Contents/MacOS/Unity}"
  mkdir -p Logs Build/Trailer

  trap 'rm -f Assets/Scripts/RingDrive.Trailer.cs Assets/Scripts/RingDrive.Trailer.cs.meta Assets/Editor/TrailerBuild.cs Assets/Editor/TrailerBuild.cs.meta' EXIT
  cp Trailer/RingDrive.Trailer.cs Trailer/RingDrive.Trailer.cs.meta Assets/Scripts/
  cp Trailer/TrailerBuild.cs Trailer/TrailerBuild.cs.meta Assets/Editor/

  "$unity_editor" -batchmode -nographics -quit \
    -projectPath "$PWD" -buildTarget StandaloneOSX \
    -executeMethod TrailerBuild.Build \
    -logFile "$PWD/Logs/trailer-build.log"
)
```

The default editor location is the project's local `Tools/Unity/` installation.
For a Unity Hub installation, set this before running the build block:

```bash
export UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity"
```

The separate capture app is `Build/Trailer/GotlandRing-Trailer.app`. It uses the
existing scene and prepared assets; this build leaves the normal macOS/Windows
release outputs and `/Applications/GotlandRing.app` alone.

## 2. Capture the gameplay and audio

```bash
GOTLAND_TRAILER_OUTPUT="$PWD/Build/Trailer/frames" \
  "Build/Trailer/GotlandRing-Trailer.app/Contents/MacOS/Gotland Ring - Impreza" \
  --trailer-capture \
  -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 \
  -logFile "$PWD/Logs/trailer-capture.log"
```

Keep graphics enabled: `-nographics` is for the editor build above, not the
capture player. The app simulates a circuit lap and a sliding sequence before
rendering, then quits automatically. The log ends with
`TRAILER_CAPTURE_DONE frames=600 duration=20s fps=30 resolution=1920x1080`.

The output directory contains:

- `frame-0000.jpg` through `frame-0599.jpg`: 600 gameplay frames.
- `engine.wav`: 20 seconds of 48 kHz stereo PCM audio.
- `shots.csv`: frame, shot, time, speed, RPM, gear, sideslip and camera distance.

For a composition preview, add `--trailer-preview` to the command and change
`GOTLAND_TRAILER_OUTPUT` to `$PWD/Build/Trailer/preview`. This writes only the
middle frame of each shot, plus the full audio and telemetry. Preview frames
cannot be encoded as the finished trailer; run the full capture afterward.

## 3. Draw the titles and encode the MP4

```bash
swift -module-cache-path /tmp/gotland-trailer-swift-cache \
  Trailer/titles.swift Build/Trailer
./Trailer/encode.sh > Logs/trailer-encode.log 2>&1
```

The Swift script writes six transparent title/matte PNGs into `Build/Trailer/`.
The encoder combines these with the gameplay frames and soundtrack, adds
fades, and writes **`docs/GotlandRing-Trailer.mp4`**, replacing that file if it
already exists. Its output uses H.264 High, 8-bit `yuv420p`, limited-range BT.709
colour, AAC stereo at 192 kbit/s and MP4 fast-start metadata.

## 4. Check the result

```bash
ffprobe -v error \
  -show_entries format=duration,size:stream=codec_name,width,height,pix_fmt,r_frame_rate,nb_frames,sample_rate,channels \
  -of json docs/GotlandRing-Trailer.mp4
ffmpeg -v error -i docs/GotlandRing-Trailer.mp4 -f null -
open docs/GotlandRing-Trailer.mp4
```

Expect a 20.000-second video with 600 frames at 1920×1080 and 30 fps, H.264
`yuv420p`, and AAC audio at 48 kHz with two channels. The full decode command
should complete without errors. Watch and listen to all five shots to check
framing, the two car models, wheel motion, readable cockpit instruments,
titles and audio transitions.

## Change the edit

| File | What to change |
|---|---|
| `RingDrive.Trailer.cs` | Recorded driving, camera positions, shot lengths, replay start times, model selection, resolution and audio mix |
| `TrailerBuild.cs` | Unity scene, target architecture and capture app destination |
| `titles.swift` | Title text, version label, typography, colours and credits |
| `encode.sh` | Overlay timing, fades, colour adjustments, compression and final filename |

In `CaptureTrailer()`, `duration` gives each shot's frame count and `start`
gives its offset into the recorded circuit or sliding sequence. Changes to
driving or camera code require rebuilding and recapturing. Title or encoding
changes can reuse existing frames and audio.

When changing the duration or frame rate, update the WAV header length, shot
frame counts, encoder duration, overlay boundaries and fades together. When
changing resolution, update both the capture dimensions and title layout.
Update the version text in `titles.swift` when capturing another game release.

`Trailer/` is source to commit. `Build/Trailer/` and `Logs/` are ignored generated
output. The temporary copies in `Assets/` are removed by the build block.

## Credits

- [Rally Car by SpatialNeglect](https://sketchfab.com/3d-models/rally-car-e0dfd3b6d19947df85002fd8de0a3a02).
- [Subaru Impreza by Mateusz Woliński](https://sketchfab.com/3d-models/subaru-impreza-7fb4298d5d8f4185b25bb2c43d7f3787).
- Both cars: [CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/).
  Models are rendered with the game's materials, driver animation and physics.
- Track: OpenStreetMap contributors (ODbL 1.0) and Lantmäteriet (CC BY 4.0).
- Original game code and prepared Impreza audio: MIT.

The trailer includes car credits on the closing card and attribution in MP4
metadata. Videos containing these car assets are for noncommercial use.
See [asset credits](../docs/ASSET-CREDITS.md), [data licences](../DATA_LICENSES.md)
and [third-party notices](../THIRD_PARTY_NOTICES.md) for full attribution.
