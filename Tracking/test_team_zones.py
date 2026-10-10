import unittest
import cv2
import numpy as np
from team_zones import ThirdsTracker, zone_index, draw_zones
from peel_patrol_bridge import performer_tracker, OriginalTracker
from bridge_runtime import encode_preview


class TeamZoneTests(unittest.TestCase):
    def test_detector_order_does_not_change_roles(self):
        tracker = ThirdsTracker()
        self.assertEqual(tracker.assign([(.8,.5),(.2,.5),(.5,.5)]), {1:1,2:2,3:0})
        self.assertEqual(tracker.assign([(.51,.5),(.81,.5),(.21,.5)]), {1:2,2:0,3:1})

    def test_each_zone_assigns_without_ever_seeing_full_group(self):
        for x, slot in ((.1,1),(.5,2),(.9,3)):
            self.assertEqual(ThirdsTracker().assign([(x,.5)]), {slot:0})

    def test_alternating_dropouts_do_not_remove_visible_roles(self):
        tracker = ThirdsTracker()
        for _ in range(100):
            self.assertEqual(tracker.assign([(.2,.5),(.8,.5)]), {1:0,3:1})
            self.assertEqual(tracker.assign([(.5,.5),(.8,.5)]), {2:0,3:1})
            self.assertEqual(tracker.assign([(.2,.5),(.5,.5)]), {1:0,2:1})
            self.assertEqual(tracker.assign([]), {})
        self.assertEqual(tracker.assign([(.2,.5),(.5,.5),(.8,.5)]), {1:0,2:1,3:2})

    def test_two_people_in_one_zone_hold_only_that_role(self):
        tracker = ThirdsTracker()
        self.assertEqual(tracker.assign([(.1,.5),(.25,.5),(.5,.5),(.8,.5)]), {2:2,3:3})
        self.assertEqual(tracker.counts, [2,1,1])
        self.assertEqual(tracker.assign([(.2,.5),(.49,.5),(.51,.5)]), {1:0})

    def test_zone_boundaries_and_out_of_frame_values(self):
        for x, expected in ((0,0),(1/3-.00001,0),(1/3,1),(2/3-.00001,1),(2/3,2),(1,2)):
            self.assertEqual(zone_index(x), expected)
        for x in (-.01,1.01,float('nan'),float('inf')):
            self.assertIsNone(zone_index(x))
        self.assertEqual(ThirdsTracker().assign([(-.1,.5),(1.1,.5),(.8,.5)]), {3:2})

    def test_lanes_never_shift_to_fit_people(self):
        tracker = ThirdsTracker()
        self.assertEqual(tracker.assign([(.1,.5),(.2,.5),(.3,.5)]), {})
        self.assertEqual(tracker.assign([(.1,.5),(.4,.5),(.7,.5)]), {1:0,2:1,3:2})
        self.assertEqual(tracker.assign([(.7,.5)]), {3:0})
        tracker.reset()
        self.assertEqual(tracker.assign([(.4,.5)]), {2:0})

    def test_other_group_modes_keep_instructor_tracker(self):
        for count in (1,2,4):
            self.assertIsInstance(performer_tracker(count), OriginalTracker)
        self.assertIsInstance(performer_tracker(3), ThirdsTracker)

    def test_guides_survive_preview_encoding_with_transport_limit(self):
        for width, height in ((320,240),(640,480),(1280,720)):
            frame = np.full((height,width,3), 65, dtype=np.uint8)
            draw_zones(frame, cv2, [1,0,2])
            encoded = encode_preview(frame, cv2)
            self.assertIsNotNone(encoded)
            self.assertLessEqual(len(encoded), 8000)
            decoded = cv2.imdecode(np.frombuffer(encoded,dtype=np.uint8), cv2.IMREAD_COLOR)
            h,w = decoded.shape[:2]
            # Both dividers must survive the same compression Unity receives.
            for split in (1,2):
                x = round(w * split / 3)
                self.assertGreater(decoded[h//2,max(0,x-2):x+3].max(), 140)


if __name__ == '__main__':
    unittest.main()
