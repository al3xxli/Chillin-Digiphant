import unittest
import cv2
import numpy as np
from types import SimpleNamespace
import peel_patrol_bridge as student


class PlayerViewTests(unittest.TestCase):
    def setUp(self):
        self.previous = (student.bridge.PerformerTracker, student.bridge.extract_features, student.bridge.MIRROR_PREVIEW, student.bridge.UNITY_RAW_PREVIEW)
        student.configure_bridge()

    def tearDown(self):
        student.bridge.PerformerTracker, student.bridge.extract_features, student.bridge.MIRROR_PREVIEW, student.bridge.UNITY_RAW_PREVIEW = self.previous

    def pose(self):
        points = [SimpleNamespace(x=.5, y=.5, z=0., visibility=1., presence=1.) for _ in range(33)]
        # Anatomical LEFT appears at camera-right before mirroring.
        for index, xy in {11:(.6,.3),12:(.4,.3),23:(.6,.6),24:(.4,.6),15:(.7,.55),16:(.3,.55),27:(.6,.9),28:(.4,.9)}.items():
            points[index].x, points[index].y = xy
        return points

    def test_raw_stream_is_not_double_mirrored_and_roles_match_unity(self):
        raw_centers = [(.8,.5),(.5,.5),(.2,.5)]
        shown = [student.bridge.preview_center(center) for center in raw_centers]
        self.assertTrue(student.bridge.MIRROR_PREVIEW)
        frame=np.arange(18,dtype=np.uint8).reshape(2,3,3)
        self.assertTrue(student.bridge.UNITY_RAW_PREVIEW)
        np.testing.assert_array_equal(student.bridge.orient_preview(frame,cv2),frame)
        self.assertEqual(student.bridge.image_center((.8,.4)),(.8,.4))
        self.assertEqual(student.performer_tracker(3).assign(shown), {1:0,2:1,3:2})
        self.assertLess(shown[0][0], shown[1][0])
        self.assertLess(shown[1][0], shown[2][0])

    def test_live_mirror_command_updates_assignment_without_flipping_source_pixels(self):
        raw=[(.8,.5),(.5,.5),(.2,.5)]
        self.assertTrue(student.bridge.apply_preview_command({'mirrorPreview':False}))
        self.assertEqual(student.performer_tracker(3).assign([student.bridge.preview_center(c) for c in raw]),{1:2,2:1,3:0})
        self.assertFalse(student.bridge.apply_preview_command({'mirrorPreview':'true'}))
        self.assertTrue(student.bridge.apply_preview_command({'mirrorPreview':True}))
        self.assertEqual(student.performer_tracker(3).assign([student.bridge.preview_center(c) for c in raw]),{1:0,2:1,3:2})
        self.assertEqual(student.bridge.image_center((.8,.5)),(.8,.5))

    def test_own_left_hand_drives_left_channel_in_both_modes(self):
        for seated in (False, True):
            points=self.pose()
            baseline=student.player_features(points,seated)[1]
            points[15].y -= .2
            raised=student.player_features(points,seated)[1]
            self.assertGreater(raised[0],baseline[0])
            self.assertEqual(raised[1],baseline[1])
            self.assertLess(student.bridge.preview_center((points[15].x,points[15].y))[0],.5)

    def test_own_right_hand_stays_on_right_input_in_both_modes(self):
        for seated in (False, True):
            points=self.pose()
            baseline=student.player_features(points,seated)[1]
            points[16].y -= .2
            raised=student.player_features(points,seated)[1]
            self.assertEqual(raised[0],baseline[0])
            self.assertGreater(raised[1],baseline[1])
            self.assertGreater(student.bridge.preview_center((points[16].x,points[16].y))[0],.5)

    def test_own_left_shoulder_down_steers_left_seated(self):
        points=self.pose();points[11].y += .1
        self.assertLess(student.player_features(points,True)[1][4],0)
        points=self.pose();points[12].y += .1
        self.assertGreater(student.player_features(points,True)[1][4],0)

    def test_own_left_lean_steers_left_full_body(self):
        points=self.pose();points[11].x += .1;points[12].x += .1
        self.assertLess(student.player_features(points,False)[1][4],0)
        points=self.pose();points[11].x -= .1;points[12].x -= .1
        self.assertGreater(student.player_features(points,False)[1][4],0)


if __name__ == '__main__':
    unittest.main()
