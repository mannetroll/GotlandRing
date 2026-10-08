# Gotland Ring - Impreza v0.2.0

Banking, variable asphalt widths and crown/hollow profiles now follow the supplied 3 m surface reconstruction around the full circuit.

- **Track surface:** preserves the original centerline and builds the road from the supplied cross-sections, with estimated banking from −4.835° to +5.778°.
- **Driving:** car contact, orientation and recovery follow the rendered surface. Banking contributes to cornering limits and downhill slip, including when reversing.
- **Auto Pilot:** plans corner speeds with banking and follows the variable asphalt boundaries.
- **Scenery:** edge paint follows the supplied boundary confidence; aprons, start markings, signs and forest heights fit the new surface.
- **Both platforms:** retains Windows x64 / Direct3D 11 and native macOS Apple Silicon / Metal builds. Both packaging scripts include the surface data and documentation.

**Verification:** both platform builds succeed. On Apple M1 Max, surface contact/recovery, driving/braking, all 42 signs and five autopilot laps pass. The best default lap was 2:59.89, with 1.34 m maximum centreline deviation. The banking update has not been runtime-tested on Windows. See [verification details](https://github.com/mannetroll/GotlandRing/blob/v0.2.0/VERIFICATION.md).

This tag contains source; v0.2.0 download packages have not been published. Build instructions are in the README.

The surface is an estimated reconstruction, not a surveyed circuit. Handling remains a simplified ground-following bicycle model without wheel-by-wheel suspension, scenery collisions or damage. See `track/SURFACE_README.md` for the source assumptions.

**Car credit:** [Rally Car](https://sketchfab.com/3d-models/rally-car-e0dfd3b6d19947df85002fd8de0a3a02) by SpatialNeglect (@jeandiz), [CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/). Builds containing this model are for noncommercial use. See the included asset credits and data licenses.
