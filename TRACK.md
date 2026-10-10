# Gotland Ring — Unity centerline

**A geospatial reconstruction of the full modern layout, not a surveyed/exact track centerline.** It includes both loops, the GP connectors and southern Mannerheim chicane. The reference photograph was used to check the route, not to generate coordinates. No authoritative downloadable surveyed centerline was found.

## Recommended Unity version: whole-lap low-pass filter

Use **`gotland_ring_full_centerline_3m_lowpass.csv`** for the revised smooth track. It has the same 2,406 rows, columns and import procedure. Horizontal coordinates, chainage, headings, horizontal curvature and the coordinate reference origin are unchanged.

The game's `Assets/Resources/Track/Surface.csv` bundles `track/gotland_ring_full_surface_3m.csv` unchanged. Its first 12 columns preserve this low-pass centerline exactly; the additional columns supply estimated asymmetric road widths, banking, crown/hollow and adjacent ground heights. No additional height filter is applied at runtime. See [SURFACE_README.md](track/SURFACE_README.md) for the surface formula, sign conventions and uncertainty. Road geometry and vehicle surface queries use the same mesh.

The original terrain heights were filtered once with a **circular, symmetric Gaussian low-pass filter**, standard deviation **15 m**, approximately 35.3 m full width at half maximum. The kernel is truncated at ±60 m and wraps across the start/finish. This introduces no spatial phase shift. The duplicate closing point is excluded during filtering and restored afterward. A constant correction preserves the distance-weighted lap mean exactly before CSV rounding. The saved CSV preserves both mean absolute elevation and mean Unity Y within 0.1 mm of the original. This is numerical preservation, not survey accuracy.

This is a conservative modeling choice for the broad road profile, not a racing-track certification rule or a suspension-scale surface model. The ideal filter retains about 89.5% of a 200 m sinusoidal variation, 98.2% at 500 m, and only 0.7% at 30 m. Mean preservation applies to the full lap; individual short sections and extrema can change. [Filter implementation documentation](https://docs.scipy.org/doc/scipy/reference/generated/scipy.ndimage.gaussian_filter1d.html).

| Check | Whole-lap filtered version |
|---|---|
| Mean elevation before / after | 32.477016 m / 32.477016 m RH 2000 |
| Mean Unity Y before / after | −6.030335 m / −6.030335 m |
| Maximum / RMS height adjustment | 1.023 m / 0.100 m |
| Filtered minimum / maximum | 19.062 m / 42.436 m RH 2000 |
| Filtered elevation range | 23.374 m |
| Horizontal / 3D length | 7214.397 m / 7216.637 m |
| Closure and coordinate checks | Passed |

Grade and cumulative 3D distance were recalculated. The vertical reference remains **38.507350922 m RH 2000**. Because row 0 itself is filtered, its local position is now approximately **(0, 0.012774, 0) m**, rather than Y=0. Keeping this fixed reference preserves mean Unity Y as well as mean elevation. The final row repeats this position exactly. Continue to omit that final duplicate when using a closed Unity spline.

See `gotland_ring_whole_lap_lowpass.png` for the full profile, close-ups and height adjustments. The original CSV is retained. These edits assume short terrain fluctuations are unwanted in the road model; they do not establish the actual road surface. The source and accuracy limits below still apply.

## Original terrain version

`gotland_ring_full_centerline_3m.csv` contains 2,406 rows (2,405 unique positions), sampled approximately every 2.9999 m. UTF-8, comma separator, decimal point, one header row, no missing values. Created 6 October 2026.

## Validation and limitations

| Check | Result |
|---|---|
| Horizontal length, sum of exported segments | 7214.397 m |
| 3D length, including terrain heights | 7218.329 m |
| Difference from the operator's 7,290 m | -75.6 m (-1.04%) |
| Elevation, RH 2000 | 18.829 to 42.514 m |
| Elevation range | 23.685 m, versus the photograph's 30 m |
| Closure / self-intersections / missing values | Exact closure / none / none |

The nominal length and advertised relief are **not reproduced exactly**. The discrepancies remain unresolved; coordinates and heights have not been stretched to force agreement. This is suitable as an approximate Unity prototype, not validated vehicle-dynamics or survey data. Horizontal accuracy is unmeasured: expect several metres of uncertainty, particularly at broad junctions and ambiguous asphalt edges. Three decimal places preserve calculations; they do not imply millimetre accuracy.

Heights represent **ground terrain**, not a dedicated track-surface survey. Banking, camber, kerbs, width and surface bumps are not modeled. The 1 m terrain catalogue lists 0.10 m vertical / 0.30 m horizontal source uncertainty over the site, but that is not the accuracy of this reconstructed track.

## Coordinates and origin

- Geographic coordinates: WGS84, EPSG:4326, decimal degrees.
- Source mapping: SWEREF99 TM, EPSG:3006. Vertical datum: **RH 2000**, EPSG:5613 (combined CRS EPSG:5845). Elevation is not WGS84 ellipsoidal height.
- Local X/Z: WGS84 azimuthal equidistant projection centered on row 0. X points east and Z north at the origin; units are metres. This avoids the roughly 3.2° grid-north rotation of raw SWEREF99 TM offsets here.
- Origin: **57.8357569254° N, 18.8308560921° E**, elevation **38.507351 m RH 2000**.
- The same origin in SWEREF99 TM: E **727401.318349**, N **6416863.884251** m.
- `y_m = elevation_m − 38.507351`. In the original terrain CSV, row 0 is `(0,0,0)` on the south main straight, proceeding ENE toward the north-loop connector. It is an arbitrary sampling origin, not a surveyed start/finish line. The filtered row 0 has the small Y offset documented above.

Projection definition: `+proj=aeqd +lat_0=57.835756925442 +lon_0=18.830856092125 +x_0=0 +y_0=0 +datum=WGS84 +units=m +no_defs +type=crs`

## Columns and import

| Column | Meaning |
|---|---|
| point_index | Zero-based row index |
| distance_m | Cumulative horizontal segment distance |
| latitude_deg, longitude_deg | WGS84 location |
| elevation_m | Absolute ground elevation, RH 2000 |
| x_m, y_m, z_m | Unity local coordinates, 1 unit = 1 metre |
| heading_deg | Clockwise from local north: 0 north, 90 east |
| grade_percent | Positive uphill in row order |
| curvature_per_m | Signed horizontal curvature: positive right turn |
| distance_3d_m | Cumulative distance including terrain elevation |

Heading, grade and curvature use a periodic 11-point cubic Savitzky–Golay fit, spanning approximately 30 m. Heights in the original terrain CSV are not smoothed; the low-pass version uses the height filter above. Optional derivative columns describe broad geometry, not suspension-scale road detail.

Copy the CSV into Unity's Assets folder and load it as a `TextAsset`. Skip the header, split each row on commas, and use columns 5, 6 and 7 for positions:

```csharp
var ci = System.Globalization.CultureInfo.InvariantCulture;
Vector3 p = new Vector3(
    float.Parse(fields[5], ci),
    float.Parse(fields[6], ci),
    float.Parse(fields[7], ci));
```

The last row repeats the first position exactly, with the final cumulative distances. For a closed Unity spline or `LineRenderer.loop = true`, **omit that last duplicate**. Otherwise keep it and connect consecutive rows. Keep the object's scale `(1,1,1)` and rotation zero to preserve the documented frame.

The saved CSV passed numeric parsing, closure, distance and coordinate round-trip checks. Unity builds run `TrackImportChecks` to verify the bundled low-pass source, point count, lengths, elevation limits and height projection. See `gotland_ring_validation.png` for the layout and `gotland_ring_whole_lap_lowpass.png` for the filtered elevation profile.

## Sources and processing

### Woodland

`Assets/Editor/ForestImport.cs` derives the forest distribution from the supplied aerial crops. The crops register at 1 m/pixel against the documented SWEREF99 TM extent: `check_north.png` starts at pixel (570, 45), `check_southwest.png` at (10, 540), `check_southeast.png` at (680, 480), and `south_grid.png` at (0, 550). Their overlapping imagery was matched to confirm these offsets. A local 3.244° grid rotation aligns the imagery with the game's east/north frame; the orange centerline overlay agrees with the supplied route markings.

Dark canopy coverage over 13 m neighbourhoods controls a deterministic, jittered 7 m planting grid. Explicit exclusions keep quarry water/shadows and the paddock open. Canopies stay at least 19 m from every centerline segment, with additional space for the track signs and game pit garages. Tree bases follow the landscape mesh and its track shoulders. The mapped extent ends at the supplied imagery; unpictured surroundings are not reconstructed.

The supplied full-lap video, kept locally at `unstaged/KOENIGSEGG.webm` outside Git, is a visual reference for the irregular, mostly low pine treeline and the contrast with open limestone areas. Tree heights of roughly 3–13.5 m, crown widths and individual positions are visual estimates, not a vegetation survey. The forest does not change track geometry or driving physics.

Both Unity build commands regenerate `Assets/Resources/Track/Forest.csv` and check road clearance, tree scale and coverage of both loops. **Gotland Ring > Prepare mapped forest** regenerates it separately. Runtime meshes group the existing pine cutout into 96 m tiles with conservative bounds for camera-facing foliage. The reference video is not bundled into the game. The forest layout is processed Lantmäteriet imagery data under CC BY 4.0; retain the attribution below.

### Track name boards

The game places 42 numbered boards using the names and approximate locations in the supplied `track/track_points.jpeg`. `Assets/Scripts/TrackLandmarks.cs` maps the photographed diagram to indices in the 3 m centerline. These are visual estimates, not surveyed sign locations. Each board stands on the driver's right, beyond the gravel runoff, and faces traffic following increasing CSV indices. Paired names share a board. The `--sign-test` checks every board's side, facing, text bounds and clearance from all track segments, and captures representative driver views.

### Centerline data

1. [GotlandRing operator](https://gotlandring.com/founders-welcome/) gives 7.29 km / 7.3 km; [configuration description](https://gotlandring.com/racing-for-skills-and-thrills/) identifies the north and south loops. Your `IMG_0284.jpeg` supplies the 30 m relief and chicane/layout check.
2. [OpenStreetMap way 311644846](https://www.openstreetmap.org/way/311644846), version 3, last edited 2022-01-28, provided the northern-loop seed. The downloaded OSM raceway only covers the old north loop (about 3.061 km). Its geometry was refined against the aerial image; the south and connectors were independently digitized from that georeferenced image.
3. [Lantmäteriet Min karta](https://www.lantmateriet.se/minkarta): official orthophoto WMS, SWEREF99 TM bbox `726600,6416300,728150,6417650`, 3100 × 2700 pixels (0.5 m/pixel); layers `Ortofoto_0.5,Ortofoto_0.4,Ortofoto_0.25,Ortofoto_0.16`. [Image catalogue](https://api.lantmateriet.se/stac-bild/v1/search?bbox=18.81,57.827,18.85,57.846&limit=20) lists April 2026, 0.16 m imagery over the site. The live WMS does not expose an immutable acquisition ID, so its exact mosaic version is unverified.
4. [National 1 m terrain tile 641_72](https://api.lantmateriet.se/stac-hojd/v1/collections/dtm-cog/items/641_72), catalogue update 2026-09-15. Its [provenance polygons](https://dl1.lantmateriet.se/hojd/pub/grid/mhm/64_7/m641_72_ursprung.json) place the full route in the **2024-02-01 airborne laser survey**. The COG download requires authentication. Heights were actually obtained through the public Min karta elevation-profile function, in short sections: 2,409 returned samples, maximum interval 3.0046 m. Returned positions were mapped to route distance and heights linearly interpolated to the export positions. A point-height spot check agreed within 0.000001 m. [Official help](https://www.lantmateriet.se/tips-minkarta) identifies the national height model and RH 2000. The service does not report a dataset revision, so identity with the catalogued tile is not independently verified.

Centerline processing: periodic spline interpolation, densified straight spans, asphalt-edge normal-profile refinement (limited to ±6 m), approximately 12 m smoothing of lateral corrections, and a final spline with 0.75 m RMS fit tolerance. Visual checks covered both loops and connectors. An independent WGS84 geodesic length check agrees with the local-coordinate length within 1 cm. No claim of survey accuracy is made.

**Attribution and reuse:** © OpenStreetMap contributors, [ODbL 1.0](https://www.openstreetmap.org/copyright). This adapted centerline database is provided under ODbL 1.0. Datakälla Lantmäteriets Min karta, © Lantmäteriet, [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/); information has been processed. Preserve these attributions and the applicable derivative-database obligations when redistributing.
