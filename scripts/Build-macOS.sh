#!/bin/bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_version="$(sed -n 's/^m_EditorVersion: //p' "$project_root/ProjectSettings/ProjectVersion.txt")"
unity_editor="${UNITY_EDITOR:-$project_root/Tools/Unity/$unity_version/Unity.app/Contents/MacOS/Unity}"
if [[ -z "${UNITY_EDITOR:-}" && ! -x "$unity_editor" ]]; then
    unity_editor="/Applications/Unity/Hub/Editor/$unity_version/Unity.app/Contents/MacOS/Unity"
fi

if [[ ! -x "$unity_editor" ]]; then
    echo "Unity $unity_version was not found at $unity_editor." >&2
    echo 'Install it through Unity Hub, or set UNITY_EDITOR to the Unity executable.' >&2
    exit 1
fi

mkdir -p "$project_root/Logs"
"$unity_editor" -batchmode -nographics -quit \
    -projectPath "$project_root" -buildTarget StandaloneOSX \
    -executeMethod BuildGame.BuildMacOS \
    -logFile "$project_root/Logs/build-macOS.log"

echo "Built $project_root/Build/macOS/GotlandRing.app"
