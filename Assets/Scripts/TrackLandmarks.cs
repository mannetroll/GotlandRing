public static class TrackLandmarks
{
 public const float KerbWidth=1.15f;
 // Visually matched to the full onboard/comparison timeline. Indices use the
 // bundled 3 m surface CSV; dimensions and endpoints are scenery estimates.
 public static readonly (string Name,int First,int Last,int Side,float FilmSeconds)[] KerbRuns={
  ("Manegaard exit",132,146,1,174),
  ("Sverige-Finland",148,174,-1,176),
  ("Nordkalk",312,345,1,13),
  ("Flying Finn",538,560,-1,33),
  ("Wilson",612,638,1,39),
  ("Senna S entry",682,719,-1,44),
  ("Senna S exit",731,762,1,48),
  ("North Shore",778,805,-1,51),
  ("Fangio approach",844,862,1,57),
  ("Altarkarusellen",915,950,-1,64),
  ("Tangentrakan",963,985,-1,68),
  ("Gotska Sandön",1000,1032,1,71),
  ("Jirhall",1041,1065,1,76),
  ("Linnamae",1103,1128,-1,80),
  ("VAV-kurvan",1157,1185,-1,85),
  ("Månen",1199,1227,1,88.5f),
  ("Havsörnen",1230,1270,-1,92),
  ("Kalk",1297,1340,1,96),
  ("Mannerheim-chikanen",1580,1605,-1,108),
  ("F.S. Krämertsskog",1870,1898,1,128),
  ("S.I-kurvan",1906,1935,1,131),
  ("S.I-kurvan exit",1950,1975,-1,133),
  ("Lönner",1992,2030,1,136),
  ("Tarmo approach",2067,2097,1,141),
  ("Tarmo-karusellen",2110,2148,-1,146),
  ("Arho",2255,2294,1,156.6f)
 };

 // Gutemålrakan: estimated reference-film timing crossing, CSV chainage 852 m.
 public const int StartFinishPoint=284;
 // Approximate positions matched from the numbered map in track/track_points.jpeg.
 // Indices refer to the unique points in the bundled 3 m low-pass centerline.
 public static readonly (int Point, string Name)[] All = {
  (12, "Vikingarakan\nViking Straight"),
  (103, "Manegaard"),
  (141, "Sverige-Finland\nKarlsöarna"),
  (250, "Gutemålrakan"),
  (307, "Nordkalk"),
  (352, "Kummikumpu"),
  (394, "Tjelvarböjen"),
  (450, "Nankang Hairpin"),
  (503, "Fabben"),
  (564, "Flying Finn"),
  (609, "Wilson\nBlåelden"),
  (710, "Senna S"),
  (778, "North Shore"),
  (831, "Cementa Bowl"),
  (862, "Fangio Apex"),
  (889, "Raukrakan"),
  (932, "Altarkarusellen"),
  (980, "Tangentrakan"),
  (1013, "Gotska Sandön"),
  (1047, "Jirhall"),
  (1092, "Linnamae"),
  (1137, "Klintberg"),
  (1178, "VAV-kurvan"),
  (1213, "Månen"),
  (1249, "Havsörnen"),
  (1305, "Kalk"),
  (1370, "Sjörakan"),
  (1438, "Svalan"),
  (1519, "Solrakan"),
  (1596, "Mannerheim-chikanen"),
  (1646, "Klippan Totte\nTottes Kurva"),
  (1704, "Havrén Sky"),
  (1762, "Vinden\nWindpower straight"),
  (1824, "The Klaus Bowl"),
  (1876, "F.S. Krämertsskog"),
  (1914, "S.I-kurvan"),
  (1987, "Lönner"),
  (2043, "Söderling-rakan"),
  (2129, "Tarmo-karusellen"),
  (2192, "Undertow"),
  (2275, "Arho"),
  (2340, "Pilgrimsfalken")
 };
}
