# Gotland Ring road surface for Unity 6

Use **gotland_ring_full_surface_3m.csv** to build a road with width and cross-slope. It extends the whole-lap low-pass centerline with 27 columns, giving **2,406 rows and 39 columns**. The original 12 columns are preserved exactly, including the mean elevation, coordinates and approximately 3 m spacing.

This is an **estimated road reconstruction**, not a surveyed replica. The video, aerial imagery and national terrain model do not supply all the data required for a physically accurate clone. Kerb geometry, barriers, full surrounding terrain, tire friction and surface roughness remain unmeasured. They are not silently filled with invented measurements.

## Import into Unity 6

1. Extract `gotland_ring_unity6_surface_kit.zip`. Copy its `GotlandRing` folder into your project's `Assets` folder.
2. Let Unity compile the supplied `Editor/GotlandRingImporter.cs`.
3. Select **gotland_ring_full_surface_3m.csv** in the Project window. Choose **Tools → Gotland Ring → Build selected surface CSV**.
4. Drag the generated `GotlandRingGenerated/GotlandRing.prefab` into your scene. Keep its position and rotation zero and its scale `(1,1,1)` to retain the documented coordinates. Place your vehicle above the surface.

The importer creates an asphalt mesh, static non-convex MeshColliders, a 3 m ground apron on each side, and simple white edge paint where boundary detection was accepted. The road has **40,902 vertices and 76,960 triangles**, with 16 subdivisions across its width. Its colors are placeholders for asphalt, pale ground and paint. The code selects a Lit shader for URP/HDRP or Standard for the built-in renderer. It creates a prefab and assets, not a complete driving game or surrounding landscape.

The apron width of **3 m**, paint width of **0.12 m**, paint offset of **0.012 m**, and material colors are explicit visual modeling choices in the importer. Apron width is the extent of the supplied local ground strip, not the actual runoff width. Its outer height uses terrain samples relative to the asphalt edge. Broad terrain, cliffs and barriers are not generated. Paint gaps at uncertain boundaries indicate missing information and do not recreate every real marking. Kerbs are not generated because their exact positions and profiles are unknown.

Rebuilding updates the generated meshes and prefab in `GotlandRingGenerated`; make your scene customization outside that generated prefab. Existing generated materials retain your edits. Use the generated road and apron names or collider references to distinguish surfaces in your vehicle controller. Unity's WheelCollider uses its own tire-friction model and does not obtain tire grip from a PhysicMaterial. No dry/wet grip coefficient was inferred from the record lap.

**Verification:** CSV parsing, original-column equality, closure, positive widths, edge reconstruction, unit normals, triangle areas/winding, and road-edge self-intersections passed. C# syntax parsing passed. A Unity Editor installation was not available for an in-engine compile, collider or driving test.

## What the video establishes

The local, Git-ignored `unstaged/KOENIGSEGG.webm` (relative to the repository root) is 203.441 seconds long at 1920 × 1080. The car is branded **Sadair's Spear**; the final card displays **02:55.88**. This is not the earlier 2:56.97 Jesko clip. Inspection covered 41 overview frames across the whole clip at roughly five-second intervals and full-resolution details at 37.5, 112.5 and 142.5 seconds.

Visible details include gray asphalt, white edge markings, alternating black-and-white kerbs in several corners, pale quarry/runoff areas, metal guardrails, pale walls with signs, wooded banks, exposed rock and wind turbines. See `gotland_ring_video_observations.json` for time-stamped observations and missing measurements. The video has not been precisely registered to CSV chainage. Camera intrinsics, mounting angle, vehicle attitude and raw telemetry are unavailable; visual camera roll is not a measured banking angle.

## Coordinates and sign conventions

- One Unity unit is one metre. **X east, Y up, Z north** at the origin. Geographic coordinates are WGS84, EPSG:4326.
- Local X/Z use WGS84 azimuthal equidistant projection centered at **57.835756925442° N, 18.830856092125° E**.
- Fixed Y reference: **38.507350922 m RH 2000**. Absolute elevation is orthometric ground height, not WGS84 ellipsoidal height.
- Source projected CRS: SWEREF99 TM, EPSG:3006. Vertical datum: RH 2000, EPSG:5613; combined EPSG:5845.
- Left and right mean left and right **looking forward in increasing row order**. Row 0 is on the south straight, not a surveyed start/finish line.
- **Positive banking means the right edge is higher than the left edge.** It supports a left turn; it is adverse banking in a right turn. Banking is the edge-to-edge angle, not necessarily the slope at the center of a crowned or hollow section.
- The final row duplicates the first cross-section, with final cumulative distances. The supplied importer uses this duplicate. A spline implementation with automatic loop closure should omit it.
- Row 0 retains the filtered center's Y offset of approximately **+0.013 m**. The centerline's distance-weighted mean elevation remains **32.477021 m** at saved CSV precision. Extending the road sideways does not preserve the area-weighted mean of an entire new surface; the preserved quantity is the original centerline mean.

## Surface formula and columns

The first 12 columns keep their definitions from `README.md`: index, horizontal distance, latitude/longitude, elevation, local XYZ, heading, grade, horizontal curvature and 3D distance.

For a signed horizontal offset `d` in metres, positive to the right:

```text
horizontal_forward = normalize(forward_x, forward_z)
horizontal_right   = (horizontal_forward.z, 0, -horizontal_forward.x)
b = cross_slope_center_percent / 100
c = cross_curve_per_m
P(d) = (x_m, y_m, z_m) + d * horizontal_right + (0, b*d + c*d*d, 0)
```

Use `d` from `-width_left_m` to `+width_right_m`. **Do not rotate this surface again by banking_deg**; the banking is already included in its heights. This polynomial is relative to the retained centerline. Its fitted absolute terrain intercept is discarded to avoid changing the centerline elevations.

| Added column(s) | Meaning |
|---|---|
| width_left_m, width_right_m | Horizontal distance from the existing reference line to each modeled asphalt edge; can be asymmetric |
| width_m | Sum of left and right horizontal widths; excludes kerbs/runoff |
| banking_deg | `atan((right_edge_y_m-left_edge_y_m)/width_m)` in degrees |
| pitch_deg | `atan(grade_percent/100)` in degrees; positive uphill |
| cross_slope_center_percent | Slope to the right at offset zero |
| cross_curve_per_m | Quadratic coefficient `c` in the formula above; units 1/m |
| crown_mid_m | Height of the road's geometric midpoint above the straight chord between its edges; positive crown, negative hollow. It is not necessarily at offset zero |
| left_edge_x_m, left_edge_y_m, left_edge_z_m | Modeled Unity position of the left edge |
| right_edge_x_m, right_edge_y_m, right_edge_z_m | Modeled Unity position of the right edge |
| forward_x, forward_y, forward_z | Unit centerline tangent, including longitudinal grade |
| normal_x, normal_y, normal_z | Unit upward surface normal at offset zero; the importer computes normals across the entire mesh |
| ground_apron_left_dy_m, ground_apron_right_dy_m | Terrain height difference from each asphalt edge to a point 3 m outward, filtered along the lap |
| width_left_inferred, width_right_inferred | 1 = weak/ambiguous detection or junction interpolation; 0 = accepted regularized image estimate, **not surveyed** |
| banking_fit_rmse_m | Interpolated quadratic-fit residual on source terrain points; **not an estimate of absolute road accuracy** |
| banking_inferred | 1 = affected by rejected cross-section interpolation; 0 = accepted terrain fit. Both are filtered terrain estimates |
| surface_id | 1 = asphalt; appearance class, not a friction coefficient |

CSV uses UTF-8, comma separators and decimal points. Parse with `InvariantCulture` and locate fields by their header names. Precision preserves calculations and does not imply millimetre-level measurements.

## Reconstruction and uncertainty

**Width:** Reused the official Lantmäteriet orthophoto at 0.5 m/pixel. Sampled intensities across horizontal normals to the existing centerline and tracked opposite asphalt-edge candidates with continuity penalties. A **12.5 m typical-width prior**, widened to **17.5 m over chainage 510–780 m** at the broad northern straight, regularizes ambiguous candidates; these priors are modeling assumptions informed by the imagery, not published width measurements. Weak edges, identified junctions and candidates below 10.5 m total width were interpolated. Final widths were smoothed over approximately 9 m. Boundaries were reviewed on a georeferenced map and an unwrapped image strip. Shadows, pavement tonal changes, kerbs and broad junctions still cause uncertainty. The inferred flags cover about **13.8% of left edges and 19.0% of right edges**. Remaining estimates are not guaranteed accurate. No independent ground-control validation exists; allow metre-scale edge uncertainty.

**Crossfall and crown:** On 8 October 2026, downloaded **481 road-interior transverse profiles**, about 15 m apart, from Lantmäteriet's public Min karta terrain-profile service. Each contained nine heights: **4,329 height samples**, maximum transverse spacing **1.878 m**. Endpoints were generally 1 m inside the estimated edges; narrower sections used a smaller inset to satisfy the service's 10 m minimum profile length. The input is ground terrain, not a dedicated pavement or mobile-LiDAR survey.

Fitted a quadratic elevation profile across each section. Rejected 22 sections with residual RMS above 0.15 m, edge-to-edge banking above 18° in magnitude, or midpoint crown/hollow above 0.6 m in magnitude. These are model-quality screening thresholds, not racing-track design standards. Applied a three-section median filter and a circular Gaussian with **15 m standard deviation**, interpolating coefficients to the 3 m CSV positions. The result is a smooth broad cross-section model; it cannot recover kerb steps, local bumps or a precise drainage crown. The estimated midpoint shape spans **−0.522 to +0.162 m**, so some sections are distinctly hollow. Those large hollows need survey validation before use in vehicle-dynamics work.

**Adjacent ground:** Another 481 transverse profiles span ±18 m, with nine points each at approximately 4.5 m intervals. These supply the apron height differences. They do not define the full runoff or surrounding terrain. In total, both transverse datasets contain **8,658 source heights**. The apron is tied to the modeled road edges for continuity and is not an independently surveyed surface.

The public profile service does not identify its exact model revision. The catalogue over this site identifies a 1 m DTM and a 2024-02-01 airborne scan. Its listed source uncertainty cannot be assigned to this processed model: centerline/edge uncertainty, interpolation, crowns, model age and smoothing all contribute additional errors.

## Validation against independent information

| Check | Result |
|---|---|
| Horizontal / centerline 3D lap length | 7,214.397 m / 7,216.637 m |
| Operator's nominal layout | 7.29–7.3 km; the approximately 76 m discrepancy remains unresolved |
| Centerline elevation range | 19.062–42.436 m RH 2000, or 23.374 m relief; the photographed 30 m claim remains unreproduced |
| Estimated width min / median / max | 10.535 / 12.151 / 17.125 m |
| Modeled edge-to-edge banking | −4.835° to +5.778° |
| Centerline columns / mean elevation | Unchanged exactly at saved precision |
| Cross-section closure | Exact at saved precision |
| Mesh geometry | No inverted or degenerate road triangles; no self-intersecting road-edge lines |

The supplied banking research led to verification of the **26 July 2004 Rejsa report claiming 10° at old section F**, the 2008 *evo* description of the banked Karussell, and the modern Rejsa discussion of outward crossfall. These are driving observations, not as-built engineering profiles. The forum itself notes that slope changes across the road. **The present edge-to-edge banking model does not reproduce 10°.** Old section F has not been reliably registered to this modern centerline, and a local slope within a curved cross-section differs from an edge-to-edge angle. No artificial 10° patch was inserted. This discrepancy remains a validation issue, not proof that either value is the current surveyed banking.

For an accurate clone, obtain georeferenced track-surface LiDAR/photogrammetry or as-built CAD/LandXML with both road edges, crossfall and kerb profiles. Full scenery additionally needs barrier/kerb positions, runoff boundaries, surrounding terrain and asset dimensions. Physical driving realism needs tire/road measurements, including wet grip and roughness; a record video cannot uniquely identify those parameters.

## Sources and attribution

- Supplied local `unstaged/KOENIGSEGG.webm` and banking research text. The video is a visual reference; no video imagery is redistributed in this kit. [Koenigsegg's Sadair's Spear page](https://www.koenigsegg.com/model/sadairs-spear) confirms the model identity and its 2025 introduction.
- [GotlandRing operator](https://gotlandring.com/founders-welcome/) and [layout description](https://gotlandring.com/racing-for-skills-and-thrills/).
- [Original 2004 Rejsa discussion](https://rejsa.nu/forum/viewtopic.php?t=11612), post dated 26 July 2004 by FredrikÅ; [modern off-camber discussion](https://rejsa.nu/forum/viewtopic.php?t=122585&start=126); [Henry Catchpole's first-hand evo report, 20 August 2008](https://www.evo.co.uk/corvette/z06/9262/corvette-z06-to-gotland-ring).
- [Lantmäteriet Min karta](https://www.lantmateriet.se/minkarta), [official height help](https://www.lantmateriet.se/tips-minkarta), [terrain catalogue tile 641_72](https://api.lantmateriet.se/stac-hojd/v1/collections/dtm-cog/items/641_72), [scan provenance](https://dl1.lantmateriet.se/hojd/pub/grid/mhm/64_7/m641_72_ursprung.json). Public elevation endpoint: `https://minkarta.lantmateriet.se/api/hojdprofil/hojdprofil/v1`. Imagery and centerline provenance are detailed in `README.md`.
- [Unity 6 Mesh API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mesh.html), [MeshCollider API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MeshCollider-sharedMesh.html), [WheelCollider friction behavior](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/WheelCollider.html).

Adapted geospatial database: © OpenStreetMap contributors, [ODbL 1.0](https://www.openstreetmap.org/copyright). Datakälla Lantmäteriets Min karta, © Lantmäteriet, [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/); information has been processed. Retain attribution and applicable derivative-database obligations. The source video and third-party articles retain their own rights. The supplied importer code may be used and modified for your Unity project.
