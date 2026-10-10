"""Student bridge entry point: three-player roles follow current screen order."""
from pathlib import Path
import sys
from team_zones import ThirdsTracker

STARTER = Path(__file__).resolve().parents[1] / "DigiPhantStarter" / "Tracking"
sys.path.insert(0, str(STARTER))
import bridge_runtime as bridge

OriginalTracker = bridge.PerformerTracker
OriginalFeatures = bridge.extract_features


def player_features(landmarks, upper_body_only=False):
    center, values, confidence = OriginalFeatures(landmarks, upper_body_only)
    # A front-facing player's own left lean must steer left (negative in Unity).
    values[4] = -values[4]
    return center, values, confidence


def performer_tracker(count):
    return ThirdsTracker() if count == 3 else OriginalTracker(count)


def configure_bridge():
    bridge.PerformerTracker = performer_tracker
    bridge.extract_features = player_features
    bridge.MIRROR_PREVIEW = True
    bridge.UNITY_RAW_PREVIEW = True


def main():
    configure_bridge()
    print("Unity mirror preview / raw image stream v2 / your own hands. LEFT P1 | CENTER P2 | RIGHT P3. "
          "One torso in each outlined third; each zone assigns independently. "
          "Recalibrate after swapping places.", flush=True)
    bridge.main()


if __name__ == "__main__":
    main()
