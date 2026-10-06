# Gotland Ring - Impreza v0.1.2

- Replaced the hand-traced circuit with the supplied 3 m centerline CSV: 2,405 unique points, approximately 7.214 km horizontally, preserving local metre coordinates.
- Track and driving heights follow the supplied terrain profile. Local smoothing removes short elevation spikes while preserving the 23.686 m overall elevation range; source CSV remains unchanged.
- Fixed trees appearing on nearby road sections at tight bends. All 820 placed trees pass road/runoff clearance checks.
- Updated minimap proportions and displayed track length.
- Retains configurable handling, reverse, FPS/frame-time HUD, curved wing, enclosed cabin and blank plates.

Download **GotlandRing-Portable.exe** for Windows x64. No Unity or .NET installation needed. The unsigned portable EXE extracts its contents to LocalAppData.

Import and smoothing checks passed, as did the CSV-track driving/braking smoke test. The centerline is a geospatial reconstruction, not a surveyed road model; widths, camber and surrounding scenery remain approximate. The origin is used as the gameplay start, not a surveyed start/finish line.

The source CSV and validation image in `track/`, plus `TRACK.md` and the license notices, are included in game builds. Attribution: OpenStreetMap contributors (ODbL 1.0); Lantmateriet Min karta (CC BY 4.0), processed information. See `TRACK.md` for full provenance.

![Updated circuit](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.1.2/docs/cockpit-hires.png)

![Impreza](https://raw.githubusercontent.com/mannetroll/GotlandRing/v0.1.2/docs/rear-v0.1.2.png)
