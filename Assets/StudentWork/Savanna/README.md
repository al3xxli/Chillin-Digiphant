# Savanna watering hole

Open `Assets/StudentWork/Scenes/Savanna-Watering-Hole.unity` and press Play.

The habitat has an open clearing, connecting sandy paths, a shallow watering hole,
acacias, shore reeds, boulders and distant rocky hills. All scenery is editable
under **Savanna habitat**. The elephant's existing rig and gesture mappings are retained.

## Explore

- **W/S** or **up/down**: walk forward/backward. **A/D** or **left/right**: steer.
- **Shift + forward**: run. Keyboard movement is available in Test sliders mode.
- Approach a colored circle, release the trunk, then **hold E** for a trunk curl.
- **Tab** opens/closes the original DigiPhant controls and sliders.
- **R** resets the elephant, activities and ball. Camera mode requires recalibration after reset.

| Place | Response |
| --- | --- |
| Teal circle / watering hole | Splash and stronger water ripples |
| Gold circle / feeding tree | Fruit is consumed, then regrows after eight seconds |
| Coral circle / play clearing | A trunk nudge rolls the ball; its interaction circle follows it |

Each action needs a fresh curl held for 0.8 seconds within its approach radius.
Holding the gesture while entering a zone cannot trigger it. Leaving the zone,
changing zones or losing tracking cancels the hold. Release before trying again.

## Camera gestures

Press **Tab**, choose **Camera**, and calibrate with the original DigiPhant controls.
The local bridge starts when Camera mode is selected. The habitat reads the existing
**Trunk curl** control's performer, movement and sensitivity, so group assignments
still apply. Gesture input drives the same actions as E; E and keyboard movement
do not override Camera mode. Switching back to Test sliders stops the preview's owned bridge.

## Editing and verification

Select **Savanna habitat** to adjust approach radii, the hold duration and references.
Selected-object gizmos show the activity ranges. Travel is bounded to 22 units;
tree trunks have horizontal avoidance. Water is shallow and walkable. The ball's
roll uses a bounded deterministic motion effect, not Rigidbody physics.

**DigiPhant > Savanna > Validate habitat** checks references, materials and gesture gating.
**Run Play mode interaction checks** exercises locomotion, all three actions,
fruit regrowth, ball movement and reset. The check resets the habitat and stops Play.
Results go to `Logs/savanna-checks.txt`. These checks use simulated inputs; live
performer calibration and gesture feel need a person in front of the camera.

The current pre-habitat scene, including unsaved edits, was preserved as
`Assets/StudentWork/Scenes/Before-Savanna-20261003-175741.unity`.
The original DigiPhant and Chillin-Digiphant scenes remain available.
