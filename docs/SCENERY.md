# Main building and roadside barriers

The main building uses the roof footprint visible just north of map label **2** in `windmills/gotland_ring_wind_turbines_map.png`. That label is CSV `point_index=2`, turbine `0980-V-006-003`.

The hall roof centre is approximately pixel (815.5, 653) in the original 2400 × 1792 map. The map's SWEREF 99 TM grid gives approximately E 727382 m, N 6416989 m. A local affine conversion fitted to the twelve supplied turbine coordinates accounts for the grid-north rotation, giving Unity X −12.1 m, Z 125.7 m. This is about 54 m east and 93 m north of turbine 2. These are image-derived estimates, not surveyed building coordinates.

The main hall is approximately 38 × 16 m, with its long axis at 82.75° clockwise from true north. A smaller northern annex and connecting wing follow the visible roof blocks. The pale walls, pitched dark metal roof, glazed bays, canopy and name board are visual approximations. Roof height and façade details are not determined by the aerial map. The forecourt is leveled near turbine 2's supplied ground elevation and blends into the game's approximate terrain with a gravel skirt.

The barrier styles follow the owner's `sound/Gotland Ring 2022 - 1 of 1.mp4`:

| Film reference | Game scenery |
|---|---|
| Around 00:18 | Northern pit straight: pale wall, diamond mesh, steel posts and inward-angled tops |
| Around 00:27 and 00:57 | Older loop: three corrugated galvanized Armco rails on steel posts |
| Around 03:05 and 03:25 | Southern extension: pale concrete barriers with a flared base |

Fourteen separate runs follow the asymmetric road edges. Their endpoints, heights and setbacks are visual estimates; the film does not provide surveyed fence locations. Runs and their reference-line indices are defined in `Assets/Scripts/RingDrive.Scenery.cs`. Bases follow the rendered shoulders and background terrain. The barriers are scenery, matching the game's existing lack of scenery collision physics.

Run a standalone player with `--scenery-test` to check rendered building placement, road clearance, barrier grounding and material support. The check captures `main-building.png`, `main-building-turbine-2.png`, `pit-catch-fence.png`, `armco-barriers.png` and `concrete-barriers.png` in the game's screenshot folder.

Map imagery: © Lantmäteriet, processed information. Turbine coordinate provenance remains in `windmills/WIND_TURBINES_README.txt`. See `DATA_LICENSES.md` for attribution.
