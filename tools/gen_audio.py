#!/usr/bin/env python3
"""Generate CaveCurse's 8-bit sound effects and the two-layer chiptune loop.

The tones are original square-wave and noise synthesis in the style of sfxr.
No samples are taken from Kenney or anywhere else. Kenney's CC0 packs are
one-shots and jingles, not a synced ambient bed plus a combat drum stem, so
this script is the placeholder music.

Run from the repo root:

    python3 tools/gen_audio.py

Output:
    audio/sfx/<cue>-<n>.wav     two or three variants of each cue
    audio/music/ambient.wav     loop that always plays
    audio/music/drums.wav       same length, silent until a fight starts
"""

from __future__ import annotations

import array
import math
import random
import wave
from pathlib import Path

SR = 44100
ROOT = Path(__file__).resolve().parents[1]
SFX = ROOT / "audio" / "sfx"
MUSIC = ROOT / "audio" / "music"


def clamp(value: float) -> int:
    return max(-32767, min(32767, int(value * 32767)))


def write_wav(path: Path, samples: list[float]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    peak = max(1e-6, max(abs(sample) for sample in samples))
    # Callers pass already-shaped levels. A safety ceiling only.
    gain = 1.0 if peak <= 1.0 else 1.0 / peak
    pcm = array.array("h", (clamp(sample * gain) for sample in samples))
    with wave.open(str(path), "w") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(SR)
        handle.writeframes(pcm.tobytes())


def crush(samples: list[float], bits: int = 5) -> list[float]:
    steps = float(2 ** (bits - 1))
    return [round(sample * steps) / steps for sample in samples]


def silence(seconds: float) -> list[float]:
    return [0.0] * int(SR * seconds)


def mix_into(target: list[float], source: list[float], at: int = 0) -> None:
    for index, sample in enumerate(source):
        pos = at + index
        if pos >= len(target):
            break
        target[pos] += sample


def square(freq: float, seconds: float, slide: float = 0.0, duty: float = 0.5) -> list[float]:
    count = int(SR * seconds)
    out = [0.0] * count
    phase = 0.0
    for index in range(count):
        t = index / max(1, count - 1)
        hz = max(20.0, freq * (1.0 + slide * t))
        phase = (phase + hz / SR) % 1.0
        out[index] = 1.0 if phase < duty else -1.0
    return out


def noise(seconds: float, rng: random.Random) -> list[float]:
    return [rng.uniform(-1.0, 1.0) for _ in range(int(SR * seconds))]


def envelope(samples: list[float], attack: float, hold: float, release: float) -> list[float]:
    count = len(samples)
    out = [0.0] * count
    for index, sample in enumerate(samples):
        t = index / SR
        if t < attack:
            gain = t / attack if attack > 0 else 1.0
        elif t < attack + hold:
            gain = 1.0
        else:
            rest = t - attack - hold
            gain = max(0.0, 1.0 - rest / release) if release > 0 else 0.0
        out[index] = sample * gain
    return out


def decay(samples: list[float], seconds: float) -> list[float]:
    out = [0.0] * len(samples)
    for index, sample in enumerate(samples):
        out[index] = sample * math.exp(-index / (seconds * SR))
    return out


def lowpass(samples: list[float], cutoff: float) -> list[float]:
    rc = 1.0 / (2.0 * math.pi * max(10.0, cutoff))
    dt = 1.0 / SR
    alpha = dt / (rc + dt)
    y = 0.0
    out = [0.0] * len(samples)
    for index, sample in enumerate(samples):
        y += alpha * (sample - y)
        out[index] = y
    return out


def scale(samples: list[float], gain: float) -> list[float]:
    return [sample * gain for sample in samples]


def concat(parts: list[list[float]]) -> list[float]:
    out: list[float] = []
    for part in parts:
        out.extend(part)
    return out


def swing(seed: int) -> list[float]:
    rng = random.Random(seed)
    seconds = 0.09 + (seed % 3) * 0.02
    body = lowpass(noise(seconds, rng), 1800 + seed * 220)
    body = envelope(body, 0.008, 0.01, seconds - 0.02)
    whoosh = square(520 + seed * 30, seconds, slide=-0.72, duty=0.25)
    whoosh = scale(envelope(whoosh, 0.004, 0.0, seconds), 0.18)
    mixed = [a + b for a, b in zip(body, whoosh)]
    return crush(scale(mixed, 0.48), 5)


def hit(seed: int) -> list[float]:
    rng = random.Random(seed)
    freq = (740, 980, 1240)[seed % 3]
    seconds = 0.07 + (seed % 3) * 0.008
    tone = square(freq, seconds, slide=-0.55, duty=0.5)
    tone = decay(tone, 0.045)
    click = decay(noise(seconds, rng), 0.012)
    mixed = [a * 0.72 + b * 0.38 for a, b in zip(tone, click)]
    return crush(scale(mixed, 0.9), 5)


def death(seed: int) -> list[float]:
    rng = random.Random(seed)
    seconds = 0.30
    boom = square(180 - seed * 8, seconds, slide=-0.86, duty=0.5)
    boom = lowpass(decay(boom, 0.16), 900)
    blast = noise(seconds, rng)
    blast = lowpass(decay(blast, 0.11 + seed * 0.005), 2400 - seed * 180)
    mixed = [a * 0.7 + b * 0.85 for a, b in zip(boom, blast)]
    return crush(scale(mixed, 0.92), 5)


def hurt(seed: int) -> list[float]:
    seconds = 0.16 + (seed % 3) * 0.015
    low = square(168 + seed * 6, seconds, slide=-0.35, duty=0.5)
    grit = square(168 * 1.5 + seed * 4, seconds, slide=-0.2, duty=0.33)
    mixed = [a * 0.75 + b * 0.28 for a, b in zip(low, grit)]
    mixed = decay(mixed, 0.09)
    return crush(scale(mixed, 0.72), 5)


def clear(seed: int) -> list[float]:
    root = (392.0, 523.25, 349.23)[seed % 3]
    notes = (root, root * 5 / 4, root * 3 / 2)
    parts: list[list[float]] = []
    for note in notes:
        tone = square(note, 0.11, slide=0.0, duty=0.5)
        tone = envelope(tone, 0.004, 0.07, 0.03)
        parts.append(crush(scale(tone, 0.62), 6))
        parts.append(silence(0.025))
    return concat(parts)


def door(seed: int) -> list[float]:
    blip_hz = (1180, 1420, 980)[seed % 3]
    blip = envelope(square(blip_hz, 0.07, slide=0.15, duty=0.5), 0.002, 0.02, 0.04)
    thump = lowpass(decay(square(92 + seed * 4, 0.18, slide=-0.4), 0.08), 280)
    count = int(SR * 0.18)
    out = [0.0] * count
    mix_into(out, scale(blip, 0.45))
    mix_into(out, scale(thump, 0.7))
    return crush(out, 5)


def pickup(seed: int) -> list[float]:
    pair = ((880, 1174), (988, 1318), (784, 1046))[seed % 3]
    parts: list[list[float]] = []
    for note in pair:
        tone = square(note, 0.055, slide=0.04, duty=0.5)
        tone = envelope(tone, 0.002, 0.02, 0.03)
        parts.append(crush(scale(tone, 0.7), 6))
        parts.append(silence(0.02))
    return concat(parts)


CUES = {
    "swing": swing,
    "hit": hit,
    "death": death,
    "hurt": hurt,
    "clear": clear,
    "door": door,
    "pickup": pickup,
}


def loop_square(freq: float, samples: int, duty: float = 0.5) -> list[float]:
    # Snap to a frequency that completes a whole number of cycles in the loop.
    cycles = max(1, round(freq * samples / SR))
    hz = cycles * SR / samples
    out = [0.0] * samples
    for index in range(samples):
        phase = (index * hz / SR) % 1.0
        out[index] = 1.0 if phase < duty else -1.0
    return out


def ambient_loop() -> list[float]:
    samples = SR * 10
    drone = scale(loop_square(73.4, samples, 0.5), 0.16)
    fifth = scale(loop_square(110.0, samples, 0.25), 0.06)
    body = lowpass([a + b for a, b in zip(drone, fifth)], 420)
    # A slow minor color on every other bar, still an integer number of cycles.
    pulse = scale(loop_square(220.0, samples, 0.5), 0.05)
    beat = int(SR * 0.625)
    for index in range(samples):
        bar_pos = (index % (beat * 8)) / (beat * 8)
        if 0.0 <= bar_pos < 0.45:
            body[index] += pulse[index]
    return crush([sample * 0.85 for sample in body], 6)


def drums_loop() -> list[float]:
    samples = SR * 10
    out = [0.0] * samples
    beat = int(SR * 0.625)
    rng = random.Random(7)
    for step in range(16):
        at = step * beat
        if step % 2 == 0:
            kick = decay(square(150, 0.12, slide=-0.82), 0.05)
            mix_into(out, scale(kick, 0.85), at)
        else:
            snare = decay(noise(0.12, rng), 0.04)
            snare = lowpass(snare, 3200)
            mix_into(out, scale(snare, 0.45), at)
        hat = decay(noise(0.04, rng), 0.012)
        hat = [sample - hat[max(0, i - 1)] for i, sample in enumerate(hat)]
        mix_into(out, scale(hat, 0.18), at)
        mix_into(out, scale(hat, 0.1), at + beat // 2)
    # Keep the loop point quiet so the next kick is the downbeat.
    tail = int(SR * 0.02)
    for index in range(tail):
        out[-1 - index] *= index / tail
    return crush(out, 5)


def main() -> None:
    for name, build in CUES.items():
        for variant in (1, 2, 3):
            write_wav(SFX / f"{name}-{variant}.wav", build(variant))
    write_wav(MUSIC / "ambient.wav", ambient_loop())
    write_wav(MUSIC / "drums.wav", drums_loop())
    print(f"wrote sfx to {SFX}")
    print(f"wrote music to {MUSIC}")


if __name__ == "__main__":
    main()
