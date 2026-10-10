"""Fixed camera-preview thirds shared by assignment and the visual guide."""
import math

SLOTS = (1, 2, 3)
LABELS = ('TRAVEL', 'STEER', 'ACTIONS')
COLORS = ((100, 230, 100), (255, 180, 80), (100, 160, 255))  # OpenCV BGR


def zone_index(x):
    if not math.isfinite(x) or not 0 <= x <= 1:
        return None
    return min(2, int(x * 3))


class ThirdsTracker:
    """Each third assigns independently; two poses in one third hold only that role."""
    def __init__(self):
        self.reset()

    def reset(self):
        self.positions = [((i + .5) / 3, .5) for i in range(3)]
        self.counts = [0, 0, 0]

    def assign(self, centers):
        candidates = [[], [], []]
        for index, center in enumerate(centers):
            zone = zone_index(center[0])
            if zone is not None:
                candidates[zone].append(index)
        self.counts = [len(group) for group in candidates]
        return {SLOTS[zone]: group[0] for zone, group in enumerate(candidates) if len(group) == 1}


def draw_zones(frame, cv2, counts, mirror=False):
    """Draw after any preview flip; guides scale with the actual preview image."""
    h, w = frame.shape[:2]
    scale = w / 640
    font = cv2.FONT_HERSHEY_SIMPLEX
    line = max(1, round(2 * scale))
    header = max(26, round(52 * scale))
    footer = max(14, round(28 * scale))
    red = (0, 0, 255)
    divider = max(4, round(8 * scale))
    cv2.rectangle(frame, (0, 0), (w - 1, header), red, -1)
    cv2.rectangle(frame, (0, h - footer), (w - 1, h - 1), (20, 24, 28), -1)
    for split in (1, 2):
        x = round(w * split / 3)
        cv2.line(frame, (x, 0), (x, h - 1), (0, 0, 0), divider + 4)
        cv2.line(frame, (x, 0), (x, h - 1), red, divider)
    cv2.rectangle(frame, (0, 0), (w - 1, h - 1), red, max(2, round(4 * scale)))
    for zone, (slot, label, color) in enumerate(zip(SLOTS, LABELS, COLORS)):
        center = (zone + .5) * w / 3
        def centered(text, y, size, ink):
            width = cv2.getTextSize(text, font, size, line)[0][0]
            cv2.putText(frame, text, (round(center - width / 2), y), font, size, ink, line, cv2.LINE_AA)
        centered(f'P{slot}  {label}', round(header * .42), .65 * scale, (255, 255, 255))
        for side, offset in ((('L' if mirror else 'R'), -.32), (('R' if mirror else 'L'), .32)):
            text_width = cv2.getTextSize(side, font, .65 * scale, line)[0][0]
            cv2.putText(frame, side, (round(center + offset * w / 3 - text_width / 2), round(header * .87)),
                        font, .65 * scale, (255, 255, 255), line, cv2.LINE_AA)
        status = 'TRACKED' if counts[zone] == 1 else 'STEP INTO VIEW' if counts[zone] == 0 else 'ONE PERSON ONLY'
        centered(status, h - round(footer * .3), .48 * scale, color if counts[zone] == 1 else (100, 210, 255))
