import math
import unittest
from types import SimpleNamespace as Point
from kart_features import body_motion, face_turn, pinch_ratio, owns_right_hand, hand_shape, shoulder_motion


def pose():
    points = [Point(x=.5, y=.5, visibility=1., presence=1.) for _ in range(33)]
    for i in (11, 12): points[i].y = .3
    for i in (23, 24): points[i].y = .55
    for i in (27, 28, 29, 30, 31, 32): points[i].y = .9
    points[11].x, points[12].x = .6, .4
    points[15].x, points[16].x = .65, .35
    return points


class KartFeatureTests(unittest.TestCase):
    def test_standing_body_uses_both_feet(self):
        body = body_motion(pose())
        self.assertAlmostEqual(body['scale'], .25)
        self.assertEqual((body['leftFootY'], body['rightFootY']), (.9, .9))

    def test_occluded_or_cropped_feet_do_not_create_body_actions(self):
        for field, value in [('visibility', .2), ('presence', .2), ('y', 1.1), ('x', -.1), ('y', math.nan)]:
            points = pose()
            setattr(points[28], field, value)
            self.assertIsNone(body_motion(points))

    def test_hand_raise_does_not_change_body_height(self):
        points = pose()
        before = body_motion(points)
        points[15].y = points[16].y = .1
        self.assertEqual(before, body_motion(points))

    def test_ankle_lift_survives_hidden_toes_and_pointed_foot(self):
        points=pose(); points[27].y=.82
        for i in (29,30,31,32):
            points[i].visibility=0; points[i].y=math.nan
        result=body_motion(points)
        self.assertAlmostEqual(result['leftFootY'],.82)
        self.assertAlmostEqual(result['rightFootY'],.9)

    def test_shrug_directions_and_relaxed_shoulders(self):
        points=pose(); self.assertEqual(shoulder_motion(points)['turn'],0)
        points[11].y-=.04
        self.assertLess(shoulder_motion(points)['turn'],0)
        points[12].y-=.08
        self.assertGreater(shoulder_motion(points)['turn'],0)

    def test_shrug_does_not_need_face_hands_hips_or_feet(self):
        points=pose()
        for i,p in enumerate(points):
            if i not in (11,12): p.visibility=0; p.y=math.nan
        self.assertEqual(shoulder_motion(points)['turn'],0)
        points[11].visibility=.2
        self.assertIsNone(shoulder_motion(points))

    def test_equal_shrug_and_head_turn_do_not_steer(self):
        points=pose(); points[11].y-=.05; points[12].y-=.05; points[0].x=.7
        self.assertEqual(shoulder_motion(points)['turn'],0)

    def test_shrug_survives_mirror_resize_and_camera_aspect(self):
        points=pose(); points[11].y-=.04
        expected=shoulder_motion(points,.75)['turn']
        for p in points: p.x=1-p.x
        self.assertAlmostEqual(shoulder_motion(points,.75)['turn'],expected)
        for p in points: p.y*=.75
        self.assertAlmostEqual(shoulder_motion(points,1)['turn'],expected)

    def test_pinch_is_relative_to_palm_size(self):
        points = [Point(x=0., y=0.) for _ in range(21)]
        points[17].x = .1
        points[8].x = .02
        self.assertAlmostEqual(pinch_ratio(points), .2)
        for p in points: p.x *= 2; p.y *= 2
        self.assertAlmostEqual(pinch_ratio(points), .2)

    def test_tiny_hand_is_rejected(self):
        self.assertIsNone(pinch_ratio([Point(x=.1, y=.1) for _ in range(21)]))

    def test_turning_own_left_is_negative(self):
        points = [Point(x=.5, y=.4) for _ in range(478)]
        points[33].x, points[263].x = .4, .6
        self.assertAlmostEqual(face_turn(points), 0)
        points[1].x = .55
        self.assertLess(face_turn(points), 0)
        points[1].x = .45
        self.assertGreater(face_turn(points), 0)

    def test_head_roll_alone_does_not_steer(self):
        points = [Point(x=.5, y=.4) for _ in range(478)]
        points[33] = Point(x=.4, y=.35)
        points[263] = Point(x=.6, y=.45)
        self.assertAlmostEqual(face_turn(points), 0)

    def test_only_assigned_right_hand_is_accepted(self):
        points = pose()
        self.assertTrue(owns_right_hand((.35, .5), points, []))
        self.assertFalse(owns_right_hand((.65, .5), points, []))
        neighbour = pose()
        self.assertFalse(owns_right_hand((.35, .5), points, [neighbour]))

    def test_invisible_neighbour_wrists_do_not_veto_a_valid_hand(self):
        points = pose(); neighbour = pose()
        neighbour[15].visibility = neighbour[16].visibility = .1
        self.assertTrue(owns_right_hand((.35, .5), points, [neighbour]))

    def hand(self, folded=()):
        points = [Point(x=0., y=0., z=0.) for _ in range(21)]
        for finger, base in enumerate((5, 9, 13, 17)):
            joints = [(0, 0), (0, 1), (0, 2), (0, 3)] if finger not in folded else [(0, 0), (0, 1), (.7, .7), (.2, .1)]
            for offset, (y, z) in enumerate(joints):
                points[base+offset] = Point(x=float(finger), y=y, z=z)
        return points

    def test_fist_requires_all_four_fingers_to_curl(self):
        self.assertEqual(hand_shape(self.hand()), dict(fist=0, pinchAllowed=True, peace=False))
        self.assertEqual(hand_shape(self.hand((0,1,2,3))), dict(fist=1, pinchAllowed=False, peace=False))
        # Thumb/index pinching can curl the index; spare fingers distinguish it.
        self.assertEqual(hand_shape(self.hand((0,))), dict(fist=0, pinchAllowed=True, peace=False))
        self.assertEqual(hand_shape(self.hand((1,2,3))), dict(fist=0, pinchAllowed=False, peace=False))

    def test_hand_shape_is_mirror_rotation_and_scale_invariant(self):
        points = self.hand((0,1,2,3)); before = hand_shape(points)
        for p in points: p.x, p.y, p.z = -3*p.z+5, 3*p.x-2, 3*p.y+7
        self.assertEqual(hand_shape(points), before)

    def test_peace_requires_index_middle_extended_and_other_two_curled(self):
        for mask in range(16):
            folded = tuple(i for i in range(4) if mask & (1 << i))
            shape = hand_shape(self.hand(folded))
            self.assertEqual(shape['peace'], folded == (2, 3), folded)
            if shape['peace']:
                self.assertFalse(shape['pinchAllowed'])
                self.assertEqual(shape['fist'], 0)

    def test_peace_survives_either_hand_rotation_scale_and_thumb_positions(self):
        for thumb in (-5, 0, 5):
            points = self.hand((2, 3))
            for i in range(1, 5): points[i].x = thumb
            for p in points: p.x, p.y, p.z = -3*p.z+5, 3*p.x-2, 3*p.y+7
            self.assertTrue(hand_shape(points)['peace'])

    def test_invalid_shape_cannot_make_a_fist(self):
        self.assertIsNone(hand_shape([Point(x=0.,y=0.) for _ in range(21)]))
        points=self.hand(); points[8].z=math.nan
        self.assertIsNone(hand_shape(points))


if __name__ == '__main__': unittest.main()
