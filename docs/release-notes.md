# Gotland Ring - Impreza v0.1.2

- Replaced the hand-traced circuit with the supplied 3 m centerline CSV: 2,405 unique points, approximately 7.214 km horizontally, preserving local metre coordinates.
- Track and driving heights use `gotland_ring_full_centerline_3m_lowpass.csv` directly, with a 23.374 m elevation range and no runtime height filtering.
- Fixed trees appearing on nearby road sections at tight bends. All 820 placed trees pass road/runoff clearance checks.
- Updated minimap proportions and displayed track length.
- Retains configurable handling, reverse, FPS/frame-time HUD, curved wing, enclosed cabin and blank plates.

Download **GotlandRing-Portable.exe** for Windows x64. No Unity or .NET installation needed. The unsigned portable EXE extracts its contents to LocalAppData.

Build checks validate the bundled low-pass source, track geometry and height projection. The centerline is a geospatial reconstruction, not a surveyed road model; widths, camber and surrounding scenery remain approximate. The origin is used as the gameplay start, not a surveyed start/finish line.

The low-pass source CSV, validation image and low-pass elevation profile in `track/`, plus `TRACK.md` and the license notices, are included in game builds. Attribution: OpenStreetMap contributors (ODbL 1.0); Lantmateriet Min karta (CC BY 4.0), processed information. See `TRACK.md` for full provenance.

![Updated circuit](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.1.2/docs/cockpit-hires.png)

![Impreza](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.1.2/docs/rear-v0.1.2.png)
