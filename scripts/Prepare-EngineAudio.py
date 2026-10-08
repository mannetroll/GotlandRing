#!/usr/bin/env python3
"""Build the game's engine loops from the owner's 2022 in-car recording.

Requires Python 3, NumPy and ffmpeg. RPM estimates come from the recording's
four exhaust pulses per 720-degree engine cycle, not recorded telemetry.
"""
import argparse
from pathlib import Path
import subprocess
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
RATE = 44100
# Resource name, source start/end in seconds, estimated reference RPM.
LOOPS = (
    ("ImprezaLow", 287.25, 289.45, 2530),
    ("ImprezaMid", 150.50, 152.30, 3060),
    ("ImprezaHigh", 234.375, 235.95, 4500),
)


def extract_engine_cycle(recorded, phase, first, cycles, rpm):
    """Keep the waveform shared by engine cycles, discarding cabin speech."""
    count = round(120 / rpm * RATE)
    positions = phase[first] + np.arange(cycles)[:, None] + np.arange(count) / count
    aligned = np.interp(positions, phase, recorded)
    aligned -= aligned.mean(axis=1, keepdims=True)
    # Speech and transient cabin sounds do not follow the crankshaft. Taking
    # the median at each engine phase rejects them; retaining a single cycle
    # also discards their syllable envelopes and changing vowel resonances.
    return np.median(aligned, axis=0)


def prepare(source, destination):
    destination.mkdir(parents=True, exist_ok=True)
    for name, start, end, rpm in LOOPS:
        decoded = subprocess.check_output([
            "ffmpeg", "-v", "error", "-ss", str(start - .4), "-i", str(source),
            "-t", str(end - start + .8), "-vn", "-ac", "1", "-ar", str(RATE),
            "-af", "highpass=f=45,highpass=f=45,lowpass=f=1600,lowpass=f=1600",
            "-f", "f32le", "-",
        ])
        recorded = np.frombuffer(decoded, dtype="<f4").astype(np.float64)
        # Follow nearby engine harmonics, then remove the slight acceleration
        # within each excerpt so that repeating a loop does not rev by itself.
        times = np.arange(.2, len(recorded) / RATE - .2, .025)
        candidates = np.linspace(rpm / 120 * .94, rpm / 120 * 1.06, 241)
        harmonics = np.arange(2, 13)
        weights = np.array([.4, .7, 1.4, .7, .4, .7, 1.2, .4, .3, .3, .4])
        hz = np.fft.rfftfreq(65536, 1 / RATE)
        fundamentals = []
        for time in times:
            center = int(time * RATE)
            frame = recorded[center - 8192:center + 8192] * np.hanning(16384)
            spectrum = np.log(np.abs(np.fft.rfft(frame, 65536)) + 1e-6)
            spectrum -= np.convolve(spectrum, np.ones(101) / 101, "same")
            scores = (np.interp(candidates[:, None] * harmonics, hz, spectrum) * weights).sum(axis=1)
            fundamentals.append(candidates[np.argmax(scores)])
        sample_times = np.arange(len(recorded)) / RATE
        frequency = np.interp(sample_times, times, fundamentals)
        phase = np.cumsum(frequency) / RATE
        first = int(.4 * RATE)
        available_cycles = phase[int((end - start + .4) * RATE)] - phase[first]
        cycles = int(available_cycles)
        loop = extract_engine_cycle(recorded, phase, first, cycles, rpm)
        # Align the dominant exhaust order across recordings, so blending two
        # RPM ranges does not repeatedly cancel and reinforce the engine note.
        spectrum = np.fft.rfft(loop)
        shift = np.angle(spectrum[4]) / 4
        loop = np.fft.irfft(spectrum * np.exp(-1j * np.arange(len(spectrum)) * shift), len(loop))
        loop -= np.mean(loop)
        loop *= min(.20 / np.sqrt(np.mean(loop * loop)), .82 / np.max(np.abs(loop)))
        pcm = np.round(loop * 32767).astype("<i2")
        with wave.open(str(destination / (name + ".wav")), "wb") as output:
            output.setparams((1, 2, RATE, len(pcm), "NONE", "not compressed"))
            output.writeframes(pcm.tobytes())
        jump = abs(float(pcm[0]) - float(pcm[-1])) / 32768
        print(f"{name}: source {start:.3f}–{end:.3f}s, {rpm} RPM, "
              f"one engine cycle from {cycles} recorded cycles, {len(pcm)} frames, "
              f"RMS={np.sqrt(np.mean(loop * loop)):.3f}, seam step={jump:.5f}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", nargs="?", type=Path,
                        default=ROOT / "sound/Gotland Ring 2022 - 1 of 1.mp4")
    parser.add_argument("--output", type=Path, default=ROOT / "Assets/Resources/Audio")
    args = parser.parse_args()
    prepare(args.source, args.output)
