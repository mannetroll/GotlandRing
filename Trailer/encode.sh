#!/bin/bash
set -euo pipefail
project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$project_root"
ffmpeg -hide_banner -y \
 -framerate 30 -i Build/Trailer/frames/frame-%04d.jpg \
 -i Build/Trailer/frames/engine.wav \
 -loop 1 -framerate 30 -i Build/Trailer/matte.png \
 -loop 1 -framerate 30 -i Build/Trailer/opening.png \
 -loop 1 -framerate 30 -i Build/Trailer/cockpit.png \
 -loop 1 -framerate 30 -i Build/Trailer/slide.png \
 -loop 1 -framerate 30 -i Build/Trailer/pass.png \
 -loop 1 -framerate 30 -i Build/Trailer/closing.png \
 -filter_complex "[0:v]eq=gamma=1.08:saturation=1.035:brightness=0.006[v0];[v0][2:v]overlay=0:0[v1];[v1][3:v]overlay=0:0:enable='lt(t,3)'[v2];[v2][4:v]overlay=0:0:enable='gte(t,3)*lt(t,8)'[v3];[v3][5:v]overlay=0:0:enable='gte(t,8)*lt(t,13)'[v4];[v4][6:v]overlay=0:0:enable='gte(t,13)*lt(t,18)'[v5];[v5][7:v]overlay=0:0:enable='gte(t,18)'[v6];[v6]fade=t=in:st=0:d=0.3,fade=t=out:st=19.55:d=0.45,scale=in_range=pc:out_range=tv:out_color_matrix=bt709,format=yuv420p[v];[1:a]volume=1.6,afade=t=in:st=0:d=0.12,afade=t=out:st=19.4:d=0.6[a]" \
 -map '[v]' -map '[a]' -t 20 -r 30 \
 -c:v libx264 -preset slow -crf 20 -maxrate 16M -bufsize 32M -profile:v high -level:v 4.1 -pix_fmt yuv420p \
 -color_range tv -color_primaries bt709 -color_trc bt709 -colorspace bt709 \
 -c:a aac -b:a 192k -ar 48000 -movflags +faststart \
 -metadata title='Gotland Ring — Impreza Gameplay Trailer' \
 -metadata artist='Mannetroll Solutions AB' \
 -metadata comment='Actual game-rendered AWD driving, captured locally on Apple M1 Max. Cars: Rally Car by SpatialNeglect and Subaru Impreza by Mateusz Woliński, CC BY-NC 4.0. Track: OpenStreetMap contributors, ODbL 1.0; Lantmäteriet, CC BY 4.0. Game source and prepared Impreza audio: MIT. Noncommercial use. github.com/mannetroll/GotlandRing' \
 docs/GotlandRing-Trailer.mp4
