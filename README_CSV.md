# Gotland Ring — Unity centerline

**A geospatial reconstruction of the full modern layout, not a surveyed/exact track centerline.** It includes both loops, the GP connectors and southern Mannerheim chicane. The reference photograph was used to check the route, not to generate coordinates. No authoritative downloadable surveyed centerline was found.

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
- `y_m = elevation_m − 38.507351`. Row 0 is `(0,0,0)` on the south main straight, proceeding ENE toward the north-loop connector. It is an arbitrary sampling origin, not a surveyed start/finish line.

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

Heading, grade and curvature use a periodic 11-point cubic Savitzky–Golay fit, spanning approximately 30 m. Heights themselves are not smoothed. Optional derivative columns describe broad geometry, not suspension-scale road detail.

Copy the CSV into Unity's Assets folder and load it as a `TextAsset`. Skip the header, split each row on commas, and use columns 5, 6 and 7 for positions:

```csharp
var ci = System.Globalization.CultureInfo.InvariantCulture;
Vector3 p = new Vector3(
    float.Parse(fields[5], ci),
    float.Parse(fields[6], ci),
    float.Parse(fields[7], ci));
```

The last row repeats the first position exactly, with the final cumulative distances. For a closed Unity spline or `LineRenderer.loop = true`, **omit that last duplicate**. Otherwise keep it and connect consecutive rows. Keep the object's scale `(1,1,1)` and rotation zero to preserve the documented frame.

The saved CSV passed numeric parsing, closure, distance and coordinate round-trip checks. No Unity project was available for an in-engine import test. See `gotland_ring_validation.png` for the layout and elevation profile.

## Sources and processing

1. [GotlandRing operator](https://gotlandring.com/founders-welcome/) gives 7.29 km / 7.3 km; [configuration description](https://gotlandring.com/racing-for-skills-and-thrills/) identifies the north and south loops. Your `IMG_0284.jpeg` supplies the 30 m relief and chicane/layout check.
2. [OpenStreetMap way 311644846](https://www.openstreetmap.org/way/311644846), version 3, last edited 2022-01-28, provided the northern-loop seed. The downloaded OSM raceway only covers the old north loop (about 3.061 km). Its geometry was refined against the aerial image; the south and connectors were independently digitized from that georeferenced image.
3. [Lantmäteriet Min karta](https://www.lantmateriet.se/minkarta): official orthophoto WMS, SWEREF99 TM bbox `726600,6416300,728150,6417650`, 3100 × 2700 pixels (0.5 m/pixel); layers `Ortofoto_0.5,Ortofoto_0.4,Ortofoto_0.25,Ortofoto_0.16`. [Image catalogue](https://api.lantmateriet.se/stac-bild/v1/search?bbox=18.81,57.827,18.85,57.846&limit=20) lists April 2026, 0.16 m imagery over the site. The live WMS does not expose an immutable acquisition ID, so its exact mosaic version is unverified.
4. [National 1 m terrain tile 641_72](https://api.lantmateriet.se/stac-hojd/v1/collections/dtm-cog/items/641_72), catalogue update 2026-09-15. Its [provenance polygons](https://dl1.lantmateriet.se/hojd/pub/grid/mhm/64_7/m641_72_ursprung.json) place the full route in the **2024-02-01 airborne laser survey**. The COG download requires authentication. Heights were actually obtained through the public Min karta elevation-profile function, in short sections: 2,409 returned samples, maximum interval 3.0046 m. Returned positions were mapped to route distance and heights linearly interpolated to the export positions. A point-height spot check agreed within 0.000001 m. [Official help](https://www.lantmateriet.se/tips-minkarta) identifies the national height model and RH 2000. The service does not report a dataset revision, so identity with the catalogued tile is not independently verified.

Centerline processing: periodic spline interpolation, densified straight spans, asphalt-edge normal-profile refinement (limited to ±6 m), approximately 12 m smoothing of lateral corrections, and a final spline with 0.75 m RMS fit tolerance. Visual checks covered both loops and connectors. An independent WGS84 geodesic length check agrees with the local-coordinate length within 1 cm. No claim of survey accuracy is made.

**Attribution and reuse:** © OpenStreetMap contributors, [ODbL 1.0](https://www.openstreetmap.org/copyright). This adapted centerline database is provided under ODbL 1.0. Datakälla Lantmäteriets Min karta, © Lantmäteriet, [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/); information has been processed. Preserve these attributions and the applicable derivative-database obligations when redistributing.
