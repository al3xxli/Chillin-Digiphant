"""Optional kart channels. Original six pose controls remain unchanged.

Measurements use the unmirrored camera image; screen mirroring only assigns roles.
No identities, images, or landmarks are stored by this module.
"""
import math


def body_motion(points):
    # Ankles remain stable when toes point down or heels disappear behind a leg.
    # Requiring all six foot landmarks discarded otherwise clear one-foot lifts.
    indices = (11, 12, 23, 24, 27, 28)
    if any(not math.isfinite(v) for i in indices for v in
           (points[i].x, points[i].y, points[i].visibility, points[i].presence)):
        return None
    quality = min(min(points[i].visibility, points[i].presence) for i in indices)
    if quality < .5 or any(not 0 <= points[i].y <= 1 or not 0 <= points[i].x <= 1 for i in indices):
        return None
    shoulder = (points[11].y + points[12].y) / 2
    hip = (points[23].y + points[24].y) / 2
    scale = abs(hip - shoulder)
    if scale < .035:
        return None
    return dict(hipY=hip, shoulderY=shoulder,
                leftFootY=points[27].y, rightFootY=points[28].y,
                scale=scale, confidence=quality)


def shoulder_motion(points, aspect=1):
    """Anatomical left shrug is negative, right positive, regardless of preview.

    Only the shoulders are required. Moving both together does not steer;
    Unity subtracts the relaxed, calibrated height difference.
    """
    if not all(visible_wrist(points[i]) for i in (11, 12)):
        return None
    left, right = points[11], points[12]
    width = abs(left.x-right.x)
    if width < .035 or not math.isfinite(aspect) or aspect <= 0:
        return None
    turn = (left.y-right.y)*aspect/width
    if abs(turn) >= 2:
        return None
    return dict(turn=turn, confidence=min(left.visibility, left.presence, right.visibility, right.presence))


def face_turn(points):
    """Nose offset projected onto the eye line removes sensitivity to head roll.

    Own left is camera right. Normalized against eye spacing, independent of
    distance and preview flip. Unity subtracts the user's neutral asymmetry.
    """
    a, b, nose = points[33], points[263], points[1]
    dx, dy = b.x - a.x, b.y - a.y
    square = dx * dx + dy * dy
    if square < .0001:
        return None
    value = -((nose.x - (a.x + b.x) / 2) * dx +
              (nose.y - (a.y + b.y) / 2) * dy) / square
    return value if math.isfinite(value) else None


def pinch_ratio(points):
    def distance(a, b):
        return math.hypot(points[a].x - points[b].x, points[a].y - points[b].y)
    palm = distance(5, 17)
    if palm < .015:
        return None
    value = distance(4, 8) / palm
    return value if math.isfinite(value) else None


def hand_shape(points, image_points=None):
    """Four-finger curl in 3D, with an upright extended thumb for jumping.

    Compare each finger's tip-to-knuckle distance with its joint-chain length.
    This is invariant to camera mirroring, hand rotation, and hand size.
    Pinching is allowed only while at least two other fingers remain extended.
    Thumbs up requires all four fingers folded and the thumb extended upward.
    Use image coordinates for upward direction, world landmarks for joint shape.
    Either hand works, including mirrored previews. Thumbs up overrides fist.
    """
    coords = [(p.x, p.y, getattr(p, 'z', 0)) for p in points]
    if len(coords) != 21 or any(not math.isfinite(v) for p in coords for v in p):
        return None
    curls = []
    folded = []
    for base in (5, 9, 13, 17):
        length = sum(math.dist(coords[i], coords[i+1]) for i in range(base, base+3))
        if length < 1e-6:
            return None
        extension = math.dist(coords[base], coords[base+3]) / length
        curls.append(max(0, min(1, (.9-extension)/.45)))
        # A relaxed folded finger can point into the palm without its tip
        # returning close to its knuckle. Measure the PIP bend as well as curl;
        # requiring tightly clenched fingers rejects a relaxed thumbs up.
        proximal = tuple(b-a for a, b in zip(coords[base], coords[base+1]))
        middle = tuple(b-a for a, b in zip(coords[base+1], coords[base+2]))
        product = math.sqrt(sum(v*v for v in proximal) * sum(v*v for v in middle))
        if product < 1e-12:
            return None
        straightness = sum(a*b for a, b in zip(proximal, middle)) / product
        folded.append(curls[-1] > .65 or straightness < .5)
    view = [(p.x, p.y, getattr(p, 'z', 0)) for p in image_points] if image_points is not None else coords
    if len(view) != 21 or any(not math.isfinite(v) for p in view for v in p):
        return None
    palm = math.dist(coords[5], coords[17])
    thumb_length = sum(math.dist(coords[i], coords[i+1]) for i in (2, 3))
    thumb_reach = math.dist(coords[2], coords[4])
    view_reach = math.hypot(view[4][0]-view[2][0], view[4][1]-view[2][1])
    thumbs_up = (all(folded) and palm > 1e-6 and thumb_length > 1e-6
                 and thumb_reach / thumb_length > .8
                 and math.dist(coords[4], coords[5]) > palm * .45
                 and view_reach > 1e-6 and view[2][1]-view[4][1] > view_reach * .5
                 and view[4][1] < view[5][1])
    return dict(fist=0 if thumbs_up else min(curls),
                pinchAllowed=not thumbs_up and sum(c < .45 for c in curls[1:]) >= 2,
                thumbsUp=thumbs_up)


def visible_wrist(point):
    return (all(math.isfinite(v) for v in (point.x, point.y, point.visibility, point.presence))
            and min(point.visibility, point.presence) >= .5
            and 0 <= point.x <= 1 and 0 <= point.y <= 1)


def owns_right_hand(wrist, pose, other_poses, aspect=1):
    """Require the hand near its owner's anatomical right wrist, not a neighbour."""
    def distance(p):
        return math.hypot(wrist[0] - p.x, (wrist[1] - p.y) * aspect)
    target = pose[16]
    if not visible_wrist(target):
        return False
    width = max(.06, math.hypot(pose[11].x - pose[12].x, (pose[11].y - pose[12].y) * aspect))
    own = distance(target)
    rivals = [pose[15]] + [p[i] for p in other_poses for i in (15, 16)]
    return own < width * .65 and all(distance(p) > own + .025 for p in rivals if visible_wrist(p))
