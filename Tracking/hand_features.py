"""Optional wrist depth channels, left then right, relative to each shoulder."""
import math


def forward_hands(landmarks):
    left, right = landmarks[11], landmarks[12]
    scale = max(math.hypot(left.x - right.x, left.y - right.y), .03)
    values, confidence = [], []
    for shoulder, wrist_index in ((left, 15), (right, 16)):
        wrist = landmarks[wrist_index]
        depth = (shoulder.z - wrist.z) / scale
        quality = min(p.visibility for p in (left, right, wrist))
        quality = min(quality, *(p.presence for p in (left, right, wrist)))
        valid = math.isfinite(depth) and math.isfinite(quality)
        values.append(max(-3., min(3., depth)) if valid else 0.)
        confidence.append(max(0., min(1., quality)) if valid else 0.)
    return dict(handForward=values, handForwardConfidence=confidence)
