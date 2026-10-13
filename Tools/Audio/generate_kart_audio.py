"""Render the original Rainbow Savannah score and kart cues, using Python + NumPy.

Run from the Unity project root. No samples, external recordings, or Nintendo
melodies are used. Both performances contain the same 32-bar original score.
The final-lap performance adds percussion and a high countervoice at 160 BPM.
"""
from pathlib import Path
import json
import wave
import numpy as np

SR = 32000
OUT = Path(__file__).resolve().parents[2] / 'Assets/StudentWork/Kart/Resources/KartAudio'
OUT.mkdir(parents=True, exist_ok=True)
RNG = np.random.default_rng(420136)


def freq(midi):
    return 440 * 2 ** ((midi - 69) / 12)


def tone(midi, duration, kind='bell'):
    t = np.arange(int(SR * duration)) / SR
    f = freq(midi)
    attack = np.minimum(1, t / .012)
    release = np.minimum(1, (duration - t) / .06)
    if kind == 'pad':
        sound = sum(np.sin(2 * np.pi * f * r * t) / a for r, a in [(1, 1), (1.003, 2.5), (.997, 2.5), (2, 6)])
        env = np.minimum(1, t / .18) * np.minimum(1, (duration - t) / .35) * .55
    elif kind == 'lead':
        vibrato = .002 * np.sin(2 * np.pi * 5.3 * t) * np.minimum(1, t / .3)
        phase = 2 * np.pi * f * (t + vibrato / (2 * np.pi * 5.3))
        sound = np.sin(phase) + .25 * np.sin(2 * phase) + .12 * np.sin(3 * phase)
        env = attack * release * (.62 + .38 * np.exp(-t * 5))
    elif kind == 'bass':
        sound = np.sin(2 * np.pi * f * t) + .23 * np.sin(4 * np.pi * f * t)
        env = attack * release * np.exp(-t * 2.5)
    else:
        sound = np.sin(2 * np.pi * f * t) * np.exp(-t * 3.4)
        sound += .38 * np.sin(2 * np.pi * f * 2 * t) * np.exp(-t * 7)
        sound += .15 * np.sin(2 * np.pi * f * 3 * t) * np.exp(-t * 11)
        env = attack * release
    return (sound * env).astype(np.float32)


def sweep(start, end, duration, noise=0):
    t = np.arange(int(SR * duration)) / SR
    phase = 2 * np.pi * (start * t + (end - start) * t * t / (2 * duration))
    return ((np.sin(phase) * .7 + RNG.standard_normal(len(t)) * noise)
            * np.sin(np.pi * t / duration) ** 2).astype(np.float32)


def percussion(kind):
    duration = {'kick': .25, 'snare': .17, 'hat': .065, 'ride': .35}[kind]
    t = np.arange(int(SR * duration)) / SR
    noise = RNG.standard_normal(len(t))
    if kind == 'kick':
        return (np.sin(2 * np.pi * (48 * t + 1.7 * (1 - np.exp(-t * 45)))) * np.exp(-t * 20)).astype(np.float32)
    if kind == 'snare':
        return ((noise * .55 + np.sin(2 * np.pi * 185 * t) * .4) * np.exp(-t * 28) * np.minimum(1, t / .001)).astype(np.float32)
    high = noise - np.roll(noise, 1)
    return (high * np.exp(-t * (70 if kind == 'hat' else 17)) * .32).astype(np.float32)


def mix(buffer, samples, at, gain, pan=0, wrap=False):
    start = round(at * SR)
    idx = np.arange(len(samples)) + start
    if wrap:
        idx %= len(buffer)
    else:
        valid = idx < len(buffer); idx = idx[valid]; samples = samples[valid]
    buffer[idx, 0] += samples * gain * np.sqrt((1 - pan) / 2)
    buffer[idx, 1] += samples * gain * np.sqrt((1 + pan) / 2)


def write(name, samples, loop=False):
    # Small stereo room + tempo-independent slap, with tails wrapped for seamless music loops.
    dry = samples.copy()
    for sec, gain in [(.113, .12), (.227, .07), (.341, .04)]:
        delay = round(sec * SR)
        echo = np.roll(dry[:, ::-1], delay, axis=0)
        if not loop:
            echo[:delay] = 0
        samples += echo * gain
    samples = np.tanh(samples * 1.18)
    peak = np.max(np.abs(samples))
    samples *= min(1, .88 / max(.001, peak))
    if not loop:
        n = min(len(samples) // 2, 320)
        samples[:n] *= np.linspace(0, 1, n)[:, None]
        samples[-n:] *= np.linspace(1, 0, n)[:, None]
    with wave.open(str(OUT / (name + '.wav')), 'wb') as file:
        file.setnchannels(2); file.setsampwidth(2); file.setframerate(SR)
        file.writeframes((samples * 32767).astype('<i2').tobytes())
    return {'seconds': round(len(samples) / SR, 3), 'peak': round(float(np.max(np.abs(samples))), 3),
            'rms': round(float(np.sqrt(np.mean(samples ** 2))), 3)}


# D major, floating added ninths, with a bright Lydian lift in the bridge.
CHORDS = [(50, 54, 57, 61, 64), (49, 52, 57, 59, 64), (47, 50, 54, 57, 61), (43, 47, 50, 54, 57),
          (52, 55, 59, 62, 66), (54, 57, 61, 64, 69), (43, 47, 50, 57, 61), (45, 49, 52, 59, 62)]
# Each pair is a pitch and its duration in beats. This is the complete authored melody.
MELODY = [
    [(78, .75), (81, .75), (85, .5), (83, 1), (81, .5), (78, .5)],
    [(76, 1), (78, .5), (81, .5), (83, 1.5), (81, .5)],
    [(78, .5), (74, .5), (78, .75), (81, .75), (85, .5), (83, 1)],
    [(83, 1), (81, .5), (78, .5), (79, 1), (78, 1)],
    [(78, .5), (79, .5), (83, 1), (86, .5), (85, .5), (83, 1)],
    [(85, .75), (88, .75), (85, .5), (81, 1), (78, 1)],
    [(79, .5), (81, .5), (83, .5), (85, .5), (86, 1), (83, 1)],
    [(85, 1), (83, .5), (81, .5), (76, 1), (73, 1)],
]


def music(bpm, urgent):
    beat = 60 / bpm
    result = np.zeros((round(32 * 4 * beat * SR), 2), np.float32)
    drums = {k: percussion(k) for k in ['kick', 'snare', 'hat', 'ride']}
    for bar in range(32):
        phrase, harmonic = divmod(bar, 8)
        chord = CHORDS[harmonic]
        start = bar * 4 * beat
        for i, pitch in enumerate(chord):
            mix(result, tone(pitch + 12, 4.25 * beat, 'pad'), start, .052, (i - 2) * .36, True)
        for step in range(8):
            pitch = chord[[0, 2, 3, 4, 2, 1, 4, 3][step]] + 24
            mix(result, tone(pitch, .6 * beat), start + step * .5 * beat, .085, (-1 if step % 2 else 1) * .48, True)
            bass = chord[0] - 12 + (12 if step in [3, 7] else 0)
            mix(result, tone(bass, .43 * beat, 'bass'), start + step * .5 * beat, .19, 0, True)
        cursor = 0
        for index, (pitch, length) in enumerate(MELODY[harmonic]):
            # The bridge lifts the first half of each phrase; the return resolves to D.
            variation = 12 if phrase == 2 and index < 3 else 0
            mix(result, tone(pitch + variation, length * beat * .94, 'lead'), start + cursor * beat, .19, -.08, True)
            if phrase in [1, 3] or urgent:
                mix(result, tone(pitch + 12, length * beat * .82), start + cursor * beat, .063, .35, True)
            cursor += length
        for quarter in range(4):
            mix(result, drums['kick'], start + quarter * beat, .24, 0, True)
            if quarter % 2:
                mix(result, drums['snare'], start + quarter * beat, .18, .08, True)
            mix(result, drums['hat'], start + (quarter + .5) * beat, .09, -.25, True)
            if urgent:
                mix(result, drums['ride'], start + quarter * beat, .08, .45, True)
                mix(result, drums['hat'], start + (quarter + .25) * beat, .05, -.4, True)
                mix(result, drums['hat'], start + (quarter + .75) * beat, .05, .4, True)
        if harmonic == 7:
            for step in range(4):
                mix(result, drums['snare'], start + (3 + step * .25) * beat, .085 + .025 * step, .1, True)
    return result


def phrase(notes, spacing=.09, duration=.22, kind='bell', gain=.42):
    result = np.zeros((round((len(notes) * spacing + duration + .4) * SR), 2), np.float32)
    for i, note in enumerate(notes):
        mix(result, tone(note, duration, kind), i * spacing, gain)
    return result


def whoosh(a, b, duration, noise=.05):
    result = np.zeros((round((duration + .3) * SR), 2), np.float32)
    mix(result, sweep(a, b, duration, noise), 0, .5)
    return result


def main():
    report = {}
    report['RainbowSavannah'] = write('RainbowSavannah', music(136, False), True)
    report['RainbowSavannahFinal'] = write('RainbowSavannahFinal', music(160, True), True)
    sequences = {
        'Countdown': ([74], .1, .14), 'Go': ([74, 78, 81, 86], .065, .3),
        'Lap': ([74, 78, 81, 86, 85, 86], .095, .26),
        'FinalLap': ([81, 81, 85, 88, 90, 93], .105, .25),
        'Finish': ([74, 78, 81, 86, 85, 81, 83, 85, 86], .145, .48),
        'Land': ([50, 62], .035, .08), 'Pickup': ([81, 85, 90], .055, .2),
        'Full': ([69, 64], .08, .12), 'Windup': ([62, 66, 69], .075, .12),
        'TargetHit': ([86, 90], .055, .16), 'Recover': ([62, 66, 69, 74], .12, .26),
        'Pause': ([78, 74], .09, .14), 'Resume': ([74, 78], .09, .14),
    }
    for name, (notes, spacing, duration) in sequences.items():
        report[name] = write(name, phrase(notes, spacing, duration))
    for name, a, b, dur, noise in [
        ('Jump', 340, 1000, .22, .025), ('Duck', 500, 170, .20, .08),
        ('Stand', 170, 430, .16, .04), ('Throw', 750, 180, .20, .2),
        ('Boost', 240, 1250, .65, .08), ('Brake', 760, 180, .24, .3),
        ('Slip', 720, 90, .65, .12), ('Hit', 120, 38, .32, .55), ('Bump', 90, 40, .15, .35),
    ]:
        report[name] = write(name, whoosh(a, b, dur, noise))
    # Integer periods preserve continuity at the boundaries of these motor/friction loops.
    t = np.arange(SR) / SR
    motor = (.12 * np.sin(2 * np.pi * 64 * t) + .055 * np.sin(2 * np.pi * 128 * t)
             + .025 * np.sin(2 * np.pi * 192 * t)) * (1 + .1 * np.sin(2 * np.pi * 8 * t))
    report['Engine'] = write('Engine', np.column_stack([motor, motor]).astype(np.float32), True)
    noise = RNG.standard_normal(SR).astype(np.float32)
    friction = (noise - np.roll(noise, 1)) * .10
    friction[:128] *= np.linspace(0, 1, 128); friction[-128:] *= np.linspace(1, 0, 128)
    report['Skid'] = write('Skid', np.column_stack([friction, friction]).astype(np.float32), True)
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
