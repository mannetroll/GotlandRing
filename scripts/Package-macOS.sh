#!/bin/bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
app="$project_root/Build/macOS/GotlandRing.app"
archive="$project_root/Build/GotlandRing-macOS-arm64.zip"

if [[ ! -d "$app" ]]; then
    echo 'Build the macOS game first with scripts/Build-macOS.sh.' >&2
    exit 1
fi
codesign --verify --deep --strict "$app"

staging="$(mktemp -d "${TMPDIR:-/tmp}/gotland-macos.XXXXXX")"
trap 'rm -rf "$staging"' EXIT
package="$staging/GotlandRing-macOS-arm64"
mkdir -p "$package"
ditto --norsrc --noextattr --noqtn "$app" "$package/GotlandRing.app"
for file in README.md LICENSE DATA_LICENSES.md THIRD_PARTY_NOTICES.md TRACK.md \
    docs/ASSET-CREDITS.md \
    track/gotland_ring_full_centerline_3m_lowpass.csv \
    track/gotland_ring_full_surface_3m.csv track/SURFACE_README.md \
    track/gotland_ring_surface_validation.png \
    track/gotland_ring_validation.png track/gotland_ring_whole_lap_lowpass.png; do
    mkdir -p "$package/$(dirname "$file")"
    cp "$project_root/$file" "$package/$file"
done

ditto -c -k --keepParent --norsrc --noextattr --noqtn "$package" "$archive"
echo "Packaged $archive"
