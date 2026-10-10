using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChillinDigiphant.Kart
{
    public sealed partial class KartRace
    {
        // Progress follows the human team's lap, never a faster rival's lap.
        public int CourseLap { get; private set; } = 1;
        readonly List<KartObstacle> lapTwoChallenges = new List<KartObstacle>();
        readonly List<KartObstacle> lapThreeChallenges = new List<KartObstacle>();
        KartObstacle[] originalObstacles;
        Transform lapChallengeRoot;
        Material bananaMaterial;
        Mesh bananaMesh;

        void InitializeLapChallenges()
        {
            originalObstacles = obstacles.ToArray();
            var log = originalObstacles.FirstOrDefault(o => o && o.kind == KartObstacleKind.Jump);
            var canopy = originalObstacles.FirstOrDefault(o => o && o.kind == KartObstacleKind.Duck);
            if (!log || !canopy) return;
            lapChallengeRoot = new GameObject("Later lap challenges / runtime").transform;
            lapChallengeRoot.SetParent(track.transform, false);
            // Build under an inactive parent so cloned physical obstacles never briefly appear.
            lapChallengeRoot.gameObject.SetActive(false);
            bananaMaterial = new Material(orangeMaterial) { name = "Banana peel yellow / runtime" };
            bananaMaterial.SetColor("_BaseColor", new Color(1, .82f, .06f));
            bananaMaterial.SetColor("_EmissionColor", new Color(.24f, .13f, 0));
            bananaMesh = KartBananaPeel.CreateMesh();
            lapTwoChallenges.Add(CloneChallenge(log, 58, 2));
            lapTwoChallenges.Add(CloneChallenge(canopy, 104, 2));
            lapTwoChallenges.Add(CreateBananas(115, 2));
            lapTwoChallenges.Add(CreateBananas(198, 2));
            lapThreeChallenges.Add(CloneChallenge(log, 18, 3));
            lapThreeChallenges.Add(CloneChallenge(canopy, 181, 3));
            lapThreeChallenges.Add(CreateBananas(92, 3));
            lapThreeChallenges.Add(CreateBananas(168, 3));
            lapChallengeRoot.gameObject.SetActive(true);
        }

        KartObstacle CloneChallenge(KartObstacle source, int sample, int lap)
        {
            var copy = Instantiate(source, lapChallengeRoot);
            copy.name = source.name + " / lap " + lap;
            copy.trackSample = sample;
            copy.transform.SetPositionAndRotation(track.Point(sample), Quaternion.LookRotation(track.Forward(sample)));
            if (copy.kind == KartObstacleKind.Duck && sample == 181)
            {
                // The downhill approach needs a little more clearance for the upright
                // chassis while crouched. Keep the branch low enough to block standing.
                const float extraClearance = .25f;
                foreach (var box in copy.GetComponents<BoxCollider>()) box.center += Vector3.up * extraClearance;
                foreach (Transform child in copy.transform)
                {
                    if (child.name == "Low branch" || child.name == "Canopy crown" || child.name == "Duck clearance marker") child.localPosition += Vector3.up * extraClearance;
                    else if (child.name == "Acacia trunk")
                    {
                        child.localPosition += Vector3.up * (extraClearance * .5f);
                        child.localScale += Vector3.up * extraClearance;
                    }
                }
            }
            // Existing warning signs were positioned in world space along the approach road.
            // Reposition them for the new station rather than carrying over the old bend.
            foreach (Transform child in copy.transform)
                if (child.name == "Advance warning post" || child.GetComponent<TextMesh>())
                    PlaceLapSign(child, copy, child.localPosition.x < 0 ? -1 : 1, child.GetComponent<TextMesh>() != null);
            copy.gameObject.SetActive(false);
            return copy;
        }

        KartObstacle CreateBananas(int sample, int lap)
        {
            var station = new GameObject("Banana crossing / lap " + lap).AddComponent<KartObstacle>();
            station.transform.SetParent(lapChallengeRoot, false);
            station.transform.SetPositionAndRotation(track.Point(sample), Quaternion.LookRotation(track.Forward(sample)));
            station.kind = KartObstacleKind.Banana;
            station.trackSample = sample;
            foreach (float lane in new[] { -5f, 0f, 5f })
            {
                var peel = new GameObject("Banana peel");
                peel.transform.SetParent(station.transform, false);
                peel.transform.localPosition = new Vector3(lane, .08f, 0);
                var trigger = peel.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = Vector3.up * .18f;
                trigger.size = new Vector3(2.4f, .36f, 2.4f);
                var visual = new GameObject("Open yellow peel");
                visual.transform.SetParent(peel.transform, false);
                visual.AddComponent<MeshFilter>().sharedMesh = bananaMesh;
                visual.AddComponent<MeshRenderer>().sharedMaterial = bananaMaterial;
                var hazard = peel.AddComponent<KartBananaPeel>();
                hazard.visual = visual;
            }
            foreach (int side in new[] { -1, 1 })
            {
                var label = new GameObject("JUMP / BANANAS").AddComponent<TextMesh>();
                label.transform.SetParent(station.transform, false);
                label.text = "JUMP\nBANANAS";
                label.fontSize = 40;
                label.characterSize = .14f;
                label.anchor = TextAnchor.MiddleCenter;
                label.color = new Color(1, .85f, .15f);
                PlaceLapSign(label.transform, station, side, true);
            }
            station.gameObject.SetActive(false);
            return station;
        }

        void PlaceLapSign(Transform part, KartObstacle obstacle, int side, bool label)
        {
            int sample = obstacle.trackSample - 6;
            part.position = track.Point(sample) + track.Right(sample) * side * (track.roadWidth * .5f + 1) + Vector3.up * (label ? 5.2f : 2.5f);
            part.rotation = label ? Quaternion.LookRotation(Vector3.ProjectOnPlane(track.Forward(sample), Vector3.up)) : Quaternion.identity;
        }

        public void AdvanceCourse(KartVehicle vehicle)
        {
            if (vehicle != player || !IsRacing || player.Finished) return;
            int lap = Mathf.Clamp(player.CompletedLaps + 1, 1, 3);
            if (lap <= CourseLap) return;
            CourseLap = lap;
            ActivateLapChallenges();
            Say("LAP " + lap + " / more jumps, low canopies and banana peels!");
        }

        void ActivateLapChallenges()
        {
            if (!IsRacing || Paused || TrackingBlocked || CourseLap < 2) return;
            bool changed = false;
            foreach (var obstacle in lapTwoChallenges.Concat(CourseLap >= 3 ? lapThreeChallenges : Enumerable.Empty<KartObstacle>()))
            {
                if (obstacle.gameObject.activeSelf) continue;
                // Never materialize a new collider inside or directly ahead of a racer.
                if (racers.Any(r => r && r.gameObject.activeInHierarchy && (r.transform.position - obstacle.transform.position).sqrMagnitude < 22 * 22)) continue;
                obstacle.gameObject.SetActive(true);
                changed = true;
            }
            if (changed) obstacles = originalObstacles.Concat(lapTwoChallenges).Concat(lapThreeChallenges).Where(o => o && o.gameObject.activeSelf).ToArray();
        }

        void ResetLapChallenges()
        {
            CourseLap = 1;
            foreach (var obstacle in lapTwoChallenges.Concat(lapThreeChallenges))
            {
                obstacle.gameObject.SetActive(false);
                foreach (var peel in obstacle.GetComponentsInChildren<KartBananaPeel>(true)) peel.ResetPeel();
            }
            if (originalObstacles != null) obstacles = originalObstacles.ToArray();
        }

        void DisposeLapChallenges()
        {
            if (lapChallengeRoot) Destroy(lapChallengeRoot.gameObject);
            if (bananaMesh) Destroy(bananaMesh);
            if (bananaMaterial) Destroy(bananaMaterial);
        }
    }
}
