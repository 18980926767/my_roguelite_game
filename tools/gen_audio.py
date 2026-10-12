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
    audio/music/ambient.wav     bass + arpeggio, always playing
    audio/music/drums.wav       melody + drums, same length, fades in during a fight

The piece is D minor, 100 BPM, 4/4, eight bars (19.2 s). Chords last two bars
each: Dm, B-flat, F, C. Both files share one sample grid so they stay in time.
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


# D minor, 100 BPM, 4/4. Eight bars is exactly 19.2 s at 44100 Hz.
BPM = 100
BEAT = SR * 60 // BPM
LOOP_BEATS = 8 * 4
LOOP_SAMPLES = LOOP_BEATS * BEAT

# The written melody is eight quarter notes. That fills two bars, so the first
# line is played twice across the first four bars. The second line fills bars
# 5–6, then D is held for the last two bars so the loop comes home.
MELODY_A = ("A4", "C5", "D5", "C5", "A4", "F4", "G4", "A4")
MELODY_B = ("F4", "G4", "A4", "C5", "Bb4", "A4", "G4", "E4")

CHORDS = (
    # root, fifth, arp: root, third, fifth, third
    ("Dm", "D2", "A2", ("D4", "F4", "A4", "F4")),
    ("Bb", "Bb2", "F3", ("Bb4", "D5", "F5", "D5")),
    ("F", "F2", "C3", ("F4", "A4", "C5", "A4")),
    ("C", "C2", "G2", ("C4", "E4", "G4", "E4")),
)


def note_hz(name: str) -> float:
    pitch = {
        "C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11,
    }
    accidental = 0
    index = 1
    if name[1:2] in ("b", "#"):
        accidental = -1 if name[1] == "b" else 1
        index = 2
    octave = int(name[index:])
    midi = (octave + 1) * 12 + pitch[name[0]] + accidental
    return 440.0 * 2.0 ** ((midi - 69) / 12.0)


def triangle(phase: float) -> float:
    if phase < 0.25:
        return phase / 0.25
    if phase < 0.75:
        return 1.0 - (phase - 0.25) / 0.25
    return -1.0 + (phase - 0.75) / 0.25


def voice(kind: str, freq: float, count: int, gain: float, duty: float = 0.5, decay_s: float | None = None) -> list[float]:
    """A note that starts and ends near zero so the loop has no click."""
    cycles = max(1, round(freq * count / SR))
    out = [0.0] * count
    attack = min(48, max(1, count // 10))
    release = min(64, max(1, count // 8))
    for index in range(count):
        phase = (index * cycles / count) % 1.0
        if kind == "tri":
            sample = triangle(phase)
        else:
            raw = 1.0 if phase < duty else -1.0
            mean = duty * 2.0 - 1.0
            peak = max(abs(1.0 - mean), abs(-1.0 - mean), 1e-6)
            sample = (raw - mean) / peak
        if decay_s:
            sample *= math.exp(-index / (decay_s * SR))
        if index < attack:
            sample *= index / attack
        if index >= count - release:
            sample *= (count - 1 - index) / release
        out[index] = sample * gain
    return out


def melody_events() -> list[tuple[str, int]]:
    events = [(name, 1) for name in MELODY_A] * 2
    events.extend((name, 1) for name in MELODY_B)
    events.append(("D4", 8))
    return events


def ambient_loop() -> list[float]:
    out = [0.0] * LOOP_SAMPLES
    eighth = BEAT // 2
    sixteenth = BEAT // 4
    for chord_index, (_label, root, fifth, arp) in enumerate(CHORDS):
        origin = chord_index * 8 * BEAT
        bass_cycle = (root, root, fifth, root)
        for step in range(16):
            mix_into(out, voice("tri", note_hz(bass_cycle[step % 4]), eighth, 0.42, decay_s=0.16), origin + step * eighth)
        for step in range(32):
            mix_into(out, voice("pulse", note_hz(arp[step % 4]), sixteenth, 0.12, duty=0.25, decay_s=0.05), origin + step * sixteenth)
    return out


def kick_hit() -> list[float]:
    count = int(SR * 0.08)
    out = [0.0] * count
    phase = 0.0
    for index in range(count):
        frac = index / count
        freq = 160.0 + (46.0 - 160.0) * frac
        phase += freq / SR
        amp = math.exp(-index / (0.022 * SR))
        if index < 16:
            amp *= index / 16
        out[index] = math.sin(2.0 * math.pi * phase) * amp * 0.55
    return out


def snare_hit() -> list[float]:
    count = int(SR * 0.09)
    rng = random.Random(11)
    burst = [rng.uniform(-1.0, 1.0) for _ in range(count)]
    burst = lowpass(burst, 3200)
    burst = [burst[index] - (burst[index - 1] if index else 0.0) for index in range(count)]
    out = [0.0] * count
    for index, sample in enumerate(burst):
        amp = math.exp(-index / (0.02 * SR))
        if index < 8:
            amp *= index / 8
        out[index] = sample * amp * 0.28
    return out


def combat_loop() -> list[float]:
    """Melody plus kick on beats 1 and 3, noise snare on beats 2 and 4."""
    out = [0.0] * LOOP_SAMPLES
    at = 0
    for name, beats in melody_events():
        count = beats * BEAT
        mix_into(out, voice("square", note_hz(name), count, 0.30, duty=0.5), at)
        at += count
    kick = kick_hit()
    snare = snare_hit()
    for beat in range(LOOP_BEATS):
        hit = kick if beat % 2 == 0 else snare
        mix_into(out, hit, beat * BEAT)
    return out


def main() -> None:
    for name, build in CUES.items():
        for variant in (1, 2, 3):
            write_wav(SFX / f"{name}-{variant}.wav", build(variant))
    if sum(beats for _name, beats in melody_events()) != LOOP_BEATS:
        raise SystemExit("melody does not fill the 8-bar loop")
    write_wav(MUSIC / "ambient.wav", ambient_loop())
    write_wav(MUSIC / "drums.wav", combat_loop())
    print(f"wrote sfx to {SFX}")
    print(f"wrote music to {MUSIC}")


if __name__ == "__main__":
    main()
