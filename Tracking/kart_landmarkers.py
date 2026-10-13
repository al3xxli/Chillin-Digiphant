"""Hand task for the assigned kart role; body and shrug steering use pose landmarks."""
from pathlib import Path
import math
from types import SimpleNamespace
from kart_features import pinch_ratio, owns_right_hand, visible_wrist, hand_shape

MODEL_DIR = Path(__file__).resolve().parent / 'models'
MODEL_URLS = {
    'hand_landmarker.task': 'https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task',
}


def install_models():
    import urllib.request
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    for name, url in MODEL_URLS.items():
        target = MODEL_DIR / name
        if not target.exists():
            temporary = target.with_suffix('.download')
            urllib.request.urlretrieve(url, temporary)
            temporary.replace(target)


class KartLandmarkers:
    def __init__(self):
        import mediapipe as mp
        from mediapipe.tasks import python
        from mediapipe.tasks.python import vision
        self.mp = mp
        self.hand = None
        try:
            self.hand = vision.HandLandmarker.create_from_options(vision.HandLandmarkerOptions(
                base_options=python.BaseOptions(model_asset_path=str(MODEL_DIR / 'hand_landmarker.task')),
                running_mode=vision.RunningMode.VIDEO, num_hands=2,
                min_hand_detection_confidence=.6, min_hand_presence_confidence=.6))
        except Exception:
            self.close()
            raise

    def close(self):
        for task in (self.hand,):
            if task:
                task.close()
        self.hand = None

    def channels(self, rgb, timestamp, assignments, observations, count, mirror):
        """P3's dedicated third accepts one hand without requiring a body.

        Other team sizes share body regions, so retain right-wrist ownership.
        """
        import numpy as np
        result = {}
        self.hand_points = []
        self.hand_status = ('P3: show one hand in the HAND section' if count == 3 else
                            f'P{count}: show your shoulders in your section')
        height, width = rgb.shape[:2]
        def crop(slot):
            independent_hand = count == 3
            if not independent_hand and slot not in assignments:
                return None
            pose = observations[assignments[slot]][3] if slot in assignments else None
            shoulder_width = max(.08, abs(pose[11].x - pose[12].x)) if pose else .08
            cx = (pose[11].x + pose[12].x) / 2 if pose else .5
            if independent_hand:
                zone = 0 if mirror else 2
                # Small overlap keeps fingers intact at the guide; the detected
                # wrist must still be inside P3's own third to take control.
                x0, x1 = zone/3 - .04, (zone+1)/3 + .04
                y0, y1 = 0, 1
            else:
                wrist = pose[16]
                if not visible_wrist(wrist):
                    self.hand_status = f'P{slot}: show your RIGHT wrist and palm'
                    return None
                # Give the palm detector a close, square view of the assigned wrist.
                # Screen thirds assign torsos, not fingers: a hand may cross a guide.
                radius = max(64, shoulder_width * width * .9)
                x0, x1 = wrist.x - radius / width, wrist.x + radius / width
                y0, y1 = wrist.y - radius / height, wrist.y + radius / height
            left, top = max(0, int(x0 * width)), max(0, int(y0 * height))
            right, bottom = min(width, int(x1 * width)), min(height, int(y1 * height))
            if right - left < 24 or bottom - top < 24:
                return None
            image = self.mp.Image(image_format=self.mp.ImageFormat.SRGB,
                                  data=np.ascontiguousarray(rgb[top:bottom, left:right]))
            return image, pose, (left / width, top / height, (right-left) / width, (bottom-top) / height)

        hand_slot = count
        item = crop(hand_slot)
        if item:
            image, pose, (x, y, w, h) = item
            detected = self.hand.detect_for_video(image, timestamp)
            hands = detected.hand_landmarks
            world_hands = getattr(detected, 'hand_world_landmarks', [])
            self.hand_status = ('P3: show one open hand in the HAND section' if count == 3 else
                                f'P{hand_slot}: open your RIGHT palm toward the camera')
            candidates = []
            others = [o[3] for i, o in enumerate(observations) if i != assignments.get(hand_slot)]
            for hand_index, points in enumerate(hands):
                full = [SimpleNamespace(x=x+p.x*w, y=y+p.y*h) for p in points]
                zone_x = 1-full[0].x if mirror else full[0].x
                accepted = (2/3 <= zone_x <= 1 and 0 <= full[0].y <= 1) if count == 3 else owns_right_hand((full[0].x, full[0].y), pose, others, height / width)
                if accepted:
                    # Normalize inside the close crop; distance from the camera must
                    # not turn a perfectly valid, small-in-frame hand into no hand.
                    local = [SimpleNamespace(x=p.x, y=p.y*h*height/(w*width), z=getattr(p, 'z', 0)) for p in points]
                    ratio = pinch_ratio(local)
                    shape = hand_shape(world_hands[hand_index] if hand_index < len(world_hands) else local, local)
                    if ratio is not None:
                        candidates.append((ratio, full, shape or dict(fist=0, pinchAllowed=False, thumbsUp=False)))
                elif count != 3:
                    self.hand_status = f'P{hand_slot}: keep your RIGHT hand separate and wrist visible'
            if len(candidates) == 1:
                ratio, self.hand_points, shape = candidates[0]
                result.setdefault(hand_slot, {})['hand'] = dict(pinch=ratio, confidence=1, **shape)
                self.hand_status = f'P{hand_slot}: hand detected'
            elif len(candidates) > 1:
                self.hand_status = f'P{hand_slot}: separate your hand from other hands'
        return result


if __name__ == '__main__':
    install_models()
    print('Kart hand model bundle installed.')
