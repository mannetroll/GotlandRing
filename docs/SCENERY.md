# Circuit scenery

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

## Quarry banks

Three pale limestone and sand banks reproduce the major quarry silhouettes visible around Månen, **25. Havsörnen** and Kalk in the locally produced comparison. Footprints in `Assets/Scripts/QuarryTerrain.cs` use the same 1 m/pixel orthophoto registration as the mapped woodland; the connecting ridge outside the Havsörnen bend is estimated from the film where the supplied aerial crops end. The eastern plateau is visible on the approach around film 01:22–01:30; the southern stockpiles form the exposed faces on the left around 01:30–01:38.

Crests, slopes and erosion detail are visual estimates from the onboard view, not surveyed quarry elevations. The banks use 4 m mesh cells, broad irregular tops and steeper rubble skirts. Bare quarry footprints exclude trees. Mesh colliders and arcade/off-road surface queries use the same triangles; the road and its entire 42 m shoulder envelope remain clear.

## Black-and-white kerbs

Twenty-six separate strips follow apexes and exits visible in `unstaged/Comparison/GotlandRing-Koenigsegg-comparison.mp4`, using its complete original onboard panel. `Assets/Scripts/RingDrive.Kerbs.cs` records each side, first/last surface row and a representative source-film time. These are elapsed video times, including the 7.6-second lead-in, rather than lap-clock readings. The replay's visual corner alignment transfers them to the supplied 3 m track sections.

Examples include Nordkalk (00:13), Altarkarusellen (01:04), Månen (01:28.5), Kalk (01:36), F.S. Krämertsskog (02:08), Tarmo-karusellen (02:26) and the right-hand edge at **41. Arho** (02:36.6, lap clock about 02:29). Separate entry and exit runs are retained where the footage shows a gap or a change of side.

Alternating weathered white and charcoal blocks are 1.5 m long and 1.15 m wide, subdivided every 0.5 m to follow corners. Positions, endpoints, dimensions and paint tones are visual modeling choices, not measurements. The mesh follows the same apron triangles as the driving surface, with a 0.025 m paint offset to prevent flicker. It represents the visible kerb markings; raised rumble profiles and kerb-specific tyre forces are not inferred from the film.

## Start and finish

The painted start/finish is on **4. Gutemålrakan**, toward Nordkalk at CSV point **284**, chainage **852 m**. This is an approximate alignment to the reference-film timing crossing around 00:07.6 / 03:03.5, supported by the pit fence and straight-to-corner layout. The footage does not establish survey-level placement. A narrow white stripe marks the line; a chequered mini-map marker locates it.

Initial spawn, Home/restart, return from training and timed laps share this location. Three ordered quarter-lap checkpoints are measured from that line. A lap finishes on a forward crossing after all three, independently of the CSV coordinate origin.

## Verification

Run a standalone player with `--scenery-test` to check rendered building placement, road clearance, barrier grounding, quarry road clearance, terrain mesh/contact agreement, kerb grounding and road-side placement, upward kerb faces and material support. The check captures `main-building.png`, `main-building-turbine-2.png`, `pit-catch-fence.png`, `armco-barriers.png`, `concrete-barriers.png`, `gutemalrakan-finish.png`, `havsornen-approach.png`, `havsornen-quarry.png`, `arho-kerb.png`, `altarkarusellen-kerb.png` and `kramertsskog-kerb.png` in the game's screenshot folder.

Map imagery: © Lantmäteriet, processed information. Turbine coordinate provenance remains in `windmills/WIND_TURBINES_README.txt`. See `DATA_LICENSES.md` for attribution.
