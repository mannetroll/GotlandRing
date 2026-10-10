"""Check that engine-cycle extraction rejects asynchronous cabin sound."""
import importlib.util
from pathlib import Path
import unittest

import numpy as np

spec = importlib.util.spec_from_file_location(
    "engine_audio", Path(__file__).with_name("Prepare-EngineAudio.py")
)
engine_audio = importlib.util.module_from_spec(spec)
spec.loader.exec_module(engine_audio)


class EngineCycleTests(unittest.TestCase):
    def test_preserves_exhaust_with_changing_voice_pitch_and_syllables(self):
        rate = engine_audio.RATE
        time = np.arange(3 * rate) / rate

        def exhaust(phase):
            return sum(amplitude * np.sin(2 * np.pi * order * phase + order * .17)
                       for order, amplitude in [(1, .04), (2, .08), (3, .1),
                                                (4, .6), (8, .22), (12, .08)])

        # Speech-like interference has an independent, changing fundamental,
        # a syllable envelope, harmonics and a brief higher-frequency sound.
        voice_phase = np.cumsum(135 + 11 * np.sin(2 * np.pi * .7 * time)) / rate
        voice = (.6 + .4 * np.sin(2 * np.pi * 3.1 * time)) * sum(
            amplitude * np.sin(2 * np.pi * order * voice_phase)
            for order, amplitude in [(1, .25), (2, .15), (3, .08), (5, .05)]
        )
        voice += .18 * np.exp(-((time - .85) / .08) ** 2) * np.sin(2 * np.pi * 710 * time)
        for rpm in (2530, 3060, 4500):
            with self.subTest(rpm=rpm):
                phase = np.cumsum(rpm / 120 * (1 + .01 * np.sin(2 * np.pi * .4 * time))) / rate
                first = int(.4 * rate)
                cycles = int(phase[int(2.6 * rate)] - phase[first])
                actual = engine_audio.extract_engine_cycle(exhaust(phase) + voice, phase, first, cycles, rpm)
                expected = exhaust(phase[first] + np.arange(len(actual)) / len(actual))
                relative_error = np.sqrt(np.mean((actual - expected) ** 2) / np.mean(expected ** 2))
                self.assertLess(relative_error, .07)
                self.assertGreater(np.corrcoef(actual, expected)[0, 1], .995)


if __name__ == "__main__":
    unittest.main()
