GOTLAND RING — WIND TURBINES FOR UNITY
Retrieved and exported: 2026-10-08

FILE TO COPY
Copy gotland_ring_wind_turbines.csv into Assets/GotlandRing/ in your Unity repository.
It contains 12 erected turbines around the circuit: six Vestas V47, three V66,
and three V90. This is an additional scenery dataset. Keep the existing road CSV.
The existing GotlandRingImporter.cs builds the road and does not read this file.

PLACEMENT
Use x_m, y_m, z_m directly as the turbine base position in metres, in the same
coordinate frame as the road. X = east, Y = up, Z = north. One Unity unit = 1 m.
Use a prefab with its pivot at ground level and its tower vertical along +Y.
If the track is moved or rotated, parent the turbines under the same root and
use these values as localPosition. Do not apply another geographic offset.

Choose the prefab using model. hub_height_m is ground-to-hub height;
rotor_diameter_m is the full swept diameter. hub_y_m and rotor_top_y_m are
absolute Y coordinates in the shared Unity frame. The latter assumes a blade
points vertically upward. Yaw, blade phase, RPM, tower taper and foundation
dimensions are not supplied by the registry and are not invented here.

SHARED ORIGIN AND DATUM
Latitude:  57.83575692544153 degrees
Longitude: 18.830856092125217 degrees
Y origin:  38.50735092163086 m, RH2000 (EPSG:5613)
Origin in SWEREF 99 TM: E 727401.3183485487, N 6416863.884250814 m.
Source horizontal CRS: SWEREF 99 TM (EPSG:3006).
Geographic columns: WGS84 (EPSG:4326), latitude then longitude.
Local projection, identical to the track:
+proj=aeqd +lat_0=57.835756925442 +lon_0=18.830856092125 +x_0=0 +y_0=0 +datum=WGS84 +units=m +no_defs +type=crs
y_m = elevation_m - 38.50735092163086.
Do not replace this fixed origin with the first filtered track elevation.
SWEREF grid coordinates are retained for provenance; subtracting grid eastings
and northings directly does not reproduce the track's local projection.

COLUMNS
point_index: 0–11; matches the accompanying map labels.
turbine_id: persistent Vindbrukskollen identifier; retain as text.
latitude_deg, longitude_deg: transformed registered position.
elevation_m: terrain height in RH2000 at that position.
x_m, y_m, z_m: Unity ground placement in the shared coordinate frame.
hub_height_m, rotor_diameter_m: dimensions reported by the official registry.
hub_y_m, rotor_top_y_m: derived Unity heights, described above.
manufacturer, model, rated_power_mw: official registry specifications.
registry_total_height_m: total height as published, without alteration.
sweref99tm_easting_m, sweref99tm_northing_m: official horizontal position.
distance_to_track_m: shortest horizontal distance to the closed track centerline.
registry_updated_date: record update date in Sweden; blank means not provided.
CSV uses UTF-8, commas, dot decimals and one header row. Parse numeric fields
using invariant culture. Blank metadata is unknown, not zero.

SOURCES AND CHECKS
1. Länsstyrelserna / Vindbrukskollen, erected-turbine layer (Uppfört):
https://ext-geodata-nationella-visning.lansstyrelsen.se/arcgis/rest/services/LST/LST_Vindbrukskollen_vindkraftverk_och_projekteringsomraden_EXT/FeatureServer/0
Public source information:
https://www.energimyndigheten.se/energisystem-och-analys/el--och-varmeproduktion/vindkraft/vindbrukskollen/
Queried bounding box in EPSG:3006: 725500,6415500,729000,6418500.
All 12 erected records returned are included; no dismantled records were
returned within this box. Eleven belong to Storugns vindpark (0980-V-006);
one belongs to Storugns 10 (0980-V-057). Registry geometry was not adjusted.

2. Lantmäteriet Min karta height profile service:
https://minkarta.lantmateriet.se/api/hojdprofil/hojdprofil/v1
For each turbine, sampled the centre of a 10.2 m east–west terrain profile
centred exactly on its registered position. All 12 samples have valid heights.
Ground elevations range from 26.812 to 41.334 m RH2000. No smoothing applied.

3. Lantmäteriet orthophotography, used to visually check all twelve locations:
https://minkarta.lantmateriet.se/map/ortofoto
Requested 0.5 m pixels, extent E 726550–728450, N 6415750–6417650.
Tower bases were checked, rather than displaced nacelles/rotor hubs in the
aerial image. Image capture date for the served mosaic was not confirmed.
The companion map includes the existing track centerline and CSV indices.

ACCURACY AND LIMITATIONS
These are authoritative registered locations, not certified surveyed positions.
The registry gives no per-point positional accuracy. Aerial checks confirm
the turbine sites, but do not establish centimetre or sub-metre accuracy.
The independent OpenStreetMap positions differ from the registry by up to
9.54 m. This is a cross-source discrepancy, not a measured error bound.
Eight decimal geographic digits and millimetre CSV decimals preserve numeric
precision during import; they do not imply that level of real-world accuracy.

Ground heights come from a terrain model, not surveyed concrete pedestal tops.
You may need to adjust the visible foundation or mesh pivot to match your
Unity terrain. The registry currently reports these turbines as erected;
most records themselves were last updated in 2019, and one has no update date.
Fetching a live registry does not certify present-day operation or dimensions.

For V47 units, the registry reports total height 78 m, while hub height 55 m
plus half the 47 m rotor diameter gives 78.5 m. Both original specifications
are preserved. rotor_top_y_m uses hub height plus rotor radius consistently.

Validation checked unique IDs, all required finite coordinates, source-point
retention, geographic/local transforms, and base/hub/tip height relationships.
Full origin, source URLs and comparison diagnostics are also supplied in
gotland_ring_wind_turbines_metadata.json.
