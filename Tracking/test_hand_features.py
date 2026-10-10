import unittest
from types import SimpleNamespace
from hand_features import forward_hands


class HandFeatureTests(unittest.TestCase):
    def pose(self):
        points = [SimpleNamespace(x=.5, y=.4, z=0., visibility=1., presence=1.) for _ in range(33)]
        points[11].x, points[12].x = .4, .6
        return points

    def test_forward_is_positive_and_hands_are_independent(self):
        points = self.pose()
        points[15].z = -.2
        result = forward_hands(points)
        self.assertAlmostEqual(result['handForward'][0], 1.)
        self.assertEqual(result['handForward'][1], 0.)
        self.assertEqual(result['handForwardConfidence'], [1., 1.])

    def test_depth_is_relative_and_scale_invariant(self):
        points = self.pose()
        points[16].z = -.1
        first = forward_hands(points)['handForward']
        for point in points:
            point.x = point.x * .5 + .1
            point.y = point.y * .5 + .1
            point.z = point.z * .5 + .2
        for a, b in zip(first, forward_hands(points)['handForward']):
            self.assertAlmostEqual(a, b)

    def test_occluded_or_invalid_wrist_is_not_an_action(self):
        points = self.pose()
        points[15].visibility = 0.
        points[16].z = float('nan')
        self.assertEqual(forward_hands(points)['handForwardConfidence'], [0., 0.])


if __name__ == '__main__':
    unittest.main()
