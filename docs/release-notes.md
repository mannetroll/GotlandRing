# Gotland Ring — asphalt tyre squeal

Download **GotlandRing-AWD-Portable.exe** and run it on Windows 11 x64. Unity and .NET are included. This is an unsigned portable preview.

- Tyres build a higher-pitched squeal as they scrub through corners, brake or spin on asphalt.
- Sound follows actual AWD tyre slip and contact load, with smooth attack and release. Airborne and off-asphalt wheels do not request squeal.
- **M** mutes the game, and pausing silences it.
- **Ctrl+P** toggles autopilot; driving keys leave it engaged. The automatic downshifts and cornering-slip setup remain in use.

**C** cycles cameras; **F3** opens handling settings; **F4** switches AWD/arcade. In manual mode, **WASD/arrows** drive and **X** reverses. **R** recovers and **Escape** pauses.

The sound is synthesized using the owner’s clips as spectral references. The **GotlandRing-Tyre-Squeal-Preview.wav** asset demonstrates the first sound tuning. See [audio implementation and checks](https://github.com/mannetroll/awd/blob/v0.3.0-awd.4/docs/ENGINE-AUDIO.md).

Audio signal checks pass at **44.1, 48 and 96 kHz**, including mute, stationary
silence, stereo agreement and headroom. The Windows AWD lap completes in
**4:19.66**, with the car remaining on the asphalt.

Car assets are for noncommercial use; attribution and track-data notices are included.
