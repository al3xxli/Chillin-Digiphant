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
        self.assertEqual(hand_shape(self.hand()), dict(fist=0, pinchAllowed=True, thumbsUp=False))
        self.assertEqual(hand_shape(self.hand((0,1,2,3))), dict(fist=1, pinchAllowed=False, thumbsUp=False))
        # Thumb/index pinching can curl the index; spare fingers distinguish it.
        self.assertEqual(hand_shape(self.hand((0,))), dict(fist=0, pinchAllowed=True, thumbsUp=False))
        self.assertEqual(hand_shape(self.hand((1,2,3))), dict(fist=0, pinchAllowed=False, thumbsUp=False))

    def test_hand_shape_is_mirror_rotation_and_scale_invariant(self):
        points = self.hand((0,1,2,3)); before = hand_shape(points)
        for p in points: p.x, p.y, p.z = -3*p.z+5, 3*p.x-2, 3*p.y+7
        self.assertEqual(hand_shape(points), before)

    def thumbs_up(self, folded=(0, 1, 2, 3), relaxed=False):
        points = self.hand(folded)
        if relaxed:
            for finger in folded:
                base = (5, 9, 13, 17)[finger]
                for offset, (y, z) in enumerate(((0, 0), (0, 1), (.65, 1), (1.15, .85))):
                    points[base+offset] = Point(x=float(finger), y=y, z=z)
        for i, y in ((2, 0), (3, -.75), (4, -1.5)):
            points[i] = Point(x=-1., y=y, z=0.)
        return points

    def test_thumbs_up_overrides_fist_and_disables_throw(self):
        for relaxed in (False, True):
            self.assertEqual(hand_shape(self.thumbs_up(relaxed=relaxed)),
                             dict(fist=0, pinchAllowed=False, thumbsUp=True))

    def test_thumbs_up_requires_all_four_fingers_folded(self):
        for mask in range(16):
            folded = tuple(i for i in range(4) if mask & (1 << i))
            self.assertEqual(hand_shape(self.thumbs_up(folded))['thumbsUp'], mask == 15)
        self.assertFalse(hand_shape(self.hand((2, 3)))['thumbsUp'], 'Peace no longer jumps')

    def test_thumbs_up_survives_mirror_scale_and_comfortable_tilt(self):
        for mirror in (-1, 1):
            for angle in (-math.pi/4, 0, math.pi/4):
                points = self.thumbs_up()
                for p in points:
                    x, y = p.x*math.cos(angle)-p.y*math.sin(angle), p.x*math.sin(angle)+p.y*math.cos(angle)
                    p.x, p.y, p.z = mirror*.02*x+.1, .02*y-.2, .02*p.z+.3
                self.assertTrue(hand_shape(points)['thumbsUp'])

    def test_sideways_down_and_tucked_thumb_do_not_jump(self):
        for angle in (math.pi/2, -math.pi/2, math.pi):
            points = self.thumbs_up()
            for p in points:
                p.x, p.y = p.x*math.cos(angle)-p.y*math.sin(angle), p.x*math.sin(angle)+p.y*math.cos(angle)
            self.assertFalse(hand_shape(points)['thumbsUp'])
        self.assertFalse(hand_shape(self.hand((0, 1, 2, 3)))['thumbsUp'])
        points = self.thumbs_up()
        points[4] = Point(x=-.1, y=.1, z=0.)
        self.assertFalse(hand_shape(points)['thumbsUp'])

    def test_thumb_direction_uses_image_landmarks(self):
        world = self.thumbs_up()
        for p in world: p.x, p.y, p.z = p.y, p.z, p.x
        self.assertTrue(hand_shape(world, self.thumbs_up())['thumbsUp'])
        view = self.thumbs_up()
        for p in view: p.y = -p.y
        self.assertFalse(hand_shape(world, view)['thumbsUp'])
        view[4].y = math.nan
        self.assertIsNone(hand_shape(world, view))

    def test_degenerate_finger_joint_is_rejected(self):
        points = self.thumbs_up()
        points[14] = points[13]
        self.assertIsNone(hand_shape(points))

    def test_invalid_shape_cannot_make_a_fist(self):
        self.assertIsNone(hand_shape([Point(x=0.,y=0.) for _ in range(21)]))
        points=self.hand(); points[8].z=math.nan
        self.assertIsNone(hand_shape(points))


if __name__ == '__main__': unittest.main()
