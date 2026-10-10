# Kart MediaPipe model bundles

Official Google MediaPipe float16 version 1 task bundles, used locally by
`Tracking/kart_landmarkers.py`. Existing pose model and pinned Python environment
remain in `DigiPhantStarter/Tracking`.

| File | SHA-256 |
| --- | --- |
| face_landmarker.task | 64184e229b263107bc2b804c6625db1341ff2bb731874b0bcc2fe6544e0bc9ff |
| hand_landmarker.task | fbc2a30080c3c557093b5ddfc334698132eb341044ccee322ccf8bcf3607cde1 |

Downloads:
- https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task
- https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task

Provider documentation and model cards:
- https://developers.google.com/edge/mediapipe/solutions/vision/face_landmarker
- https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker

Restore missing bundles with `DigiPhantStarter/Tracking/.venv/Scripts/python.exe
Tracking/kart_landmarkers.py` from the Unity project root, or run the main
`Setup-DigiPhant.ps1`. The installer preserves existing files. To repair a
corrupt bundle, move it aside before running the installer again.
