"""Check that each model receives pixels from its assigned player, in either preview orientation."""
import unittest
from types import SimpleNamespace as P
from unittest.mock import Mock
import numpy as np
from kart_landmarkers import KartLandmarkers
from team_zones import ThirdsTracker


class KartRoleCropTests(unittest.TestCase):
    def test_face_model_is_unused_and_hand_uses_screen_right_in_both_orientations(self):
        for mirror in (False, True):
            with self.subTest(mirror=mirror):
                image = np.zeros((400, 600, 3), dtype=np.uint8)
                colors = [(70, 0, 0), (0, 140, 0), (0, 0, 210)]
                observations = []
                centers = []
                for zone, x in enumerate((1/6, .5, 5/6)):
                    image[:, zone*200:(zone+1)*200] = colors[zone]
                    points = [P(x=x, y=.2, visibility=1, presence=1) for _ in range(33)]
                    points[11] = P(x=x-.06, y=.4)
                    points[12] = P(x=x+.06, y=.4)
                    observations.append((None, None, None, points))
                    centers.append((1-x if mirror else x, .5))
                assignments = ThirdsTracker().assign(centers)
                tasks = KartLandmarkers.__new__(KartLandmarkers)
                tasks.mp = P(Image=lambda **args: args['data'], ImageFormat=P(SRGB=1))
                tasks.face = P(detect_for_video=Mock(return_value=P(face_landmarks=[])))
                tasks.hand = P(detect_for_video=Mock(return_value=P(hand_landmarks=[])))
                tasks.channels(image, 10, assignments, observations, 3, mirror)
                hand_pixels = tasks.hand.detect_for_video.call_args.args[0]
                self.assertFalse(tasks.face.detect_for_video.called, 'Shoulder steering must never run the face model')
                self.assertTrue(np.all(hand_pixels[:, 30:-30] == colors[0 if mirror else 2]), 'P3 crop follows displayed right third, with a small finger margin')

    def test_hand_only_role_needs_no_pose_in_either_orientation(self):
        for mirror in (False, True):
            for number in (0, 1, 2):
                with self.subTest(mirror=mirror, hands=number):
                    tasks = KartLandmarkers.__new__(KartLandmarkers)
                    tasks.mp = P(Image=lambda **args: args['data'], ImageFormat=P(SRGB=1))
                    tasks.face = P(detect_for_video=Mock())
                    points = [P(x=.5, y=.5) for _ in range(21)]
                    points[5].x=.4; points[17].x=.6; points[8].x=.65
                    tasks.hand = P(detect_for_video=Mock(return_value=P(hand_landmarks=[points]*number)))
                    result = tasks.channels(np.zeros((480, 640, 3), dtype=np.uint8), 10, {}, [], 3, mirror)
                    self.assertEqual('hand' in result.get(3, {}), number == 1)
                    self.assertFalse(tasks.face.detect_for_video.called)

    def test_neighbour_hand_in_crop_margin_cannot_control_p3(self):
        tasks = KartLandmarkers.__new__(KartLandmarkers)
        tasks.mp = P(Image=lambda **args: args['data'], ImageFormat=P(SRGB=1))
        tasks.face = P(detect_for_video=Mock())
        points = [P(x=.01, y=.5) for _ in range(21)]
        points[5].x=.2; points[17].x=.4
        tasks.hand = P(detect_for_video=Mock(return_value=P(hand_landmarks=[points])))
        self.assertEqual(tasks.channels(np.zeros((480,640,3),dtype=np.uint8),10,{},[],3,False), {})


if __name__ == '__main__':
    unittest.main()
