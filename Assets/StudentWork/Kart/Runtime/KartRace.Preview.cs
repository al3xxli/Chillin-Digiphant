using DigiPhant;
using UnityEngine;

namespace ChillinDigiphant.Kart
{
    public sealed partial class KartRace
    {
        sealed class PreviewSizing
        {
            public readonly string key;
            public Vector2 size, dragStart, sizeStart;
            public PreviewSizing(string name, Vector2 initial)
            {
                key = "Kart.CameraPreview." + name;
                size = new Vector2(PlayerPrefs.GetFloat(key + ".Width", initial.x), PlayerPrefs.GetFloat(key + ".Height", initial.y));
            }
            public void Save()
            {
                PlayerPrefs.SetFloat(key + ".Width", size.x); PlayerPrefs.SetFloat(key + ".Height", size.y); PlayerPrefs.Save();
            }
        }
        PreviewSizing gamePreview, dockPreview;
        PreviewSizing GamePreview => gamePreview ?? (gamePreview = new PreviewSizing("Game", new Vector2(260, 195)));
        PreviewSizing DockPreview => dockPreview ?? (dockPreview = new PreviewSizing("Dock", new Vector2(360, 270)));
        public Rect LastPreviewHandle { get; private set; }
        public Rect LastPreviewRect { get; private set; }
        public Vector2 DockPreviewSize => DockPreview.size;

        void DrawResizablePreview(DigiPhantCameraPreview preview, float width, bool inGame)
        {
            var sizing = inGame ? GamePreview : DockPreview;
            float maxWidth = inGame ? Screen.width * .55f / UiScale - 58 : width - 30;
            Vector2 maximum = new Vector2(Mathf.Max(160, maxWidth), 600);
            Vector2 drawn = new Vector2(Mathf.Clamp(sizing.size.x, 160, maximum.x), Mathf.Clamp(sizing.size.y, 100, maximum.y));
            Rect picture = GUILayoutUtility.GetRect(drawn.x, drawn.y, GUILayout.Width(drawn.x), GUILayout.Height(drawn.y));
            Fill(picture, Color.black);
            if (preview.HasLiveFrame) preview.DrawPreview(picture);
            else GUI.Label(new Rect(picture.x + 10, picture.y + 20, picture.width - 20, picture.height - 25), "1. Start camera\n2. P1 body / P2 shoulders / P3 hand\n3. Set neutral pose", note);
            if (preview.HasLiveFrame && controller.performerCount == 3) DrawZones(picture, preview.PreviewTexture);

            Rect handle = new Rect(picture.xMax - 24, picture.yMax - 24, 24, 24);
            int id = GUIUtility.GetControlID(sizing.key.GetHashCode(), FocusType.Passive);
            Event e = Event.current;
            if (e.type == EventType.Repaint && !inGame)
            {
                LastPreviewHandle = new Rect(GUIUtility.GUIToScreenPoint(handle.position), handle.size);
                LastPreviewRect = new Rect(GUIUtility.GUIToScreenPoint(picture.position), picture.size);
            }
#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.AddCursorRect(handle, UnityEditor.MouseCursor.ResizeUpLeft);
#endif
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && handle.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id; sizing.dragStart = GUIUtility.GUIToScreenPoint(e.mousePosition); sizing.sizeStart = drawn; e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        Vector2 delta = (GUIUtility.GUIToScreenPoint(e.mousePosition) - sizing.dragStart) / (inGame ? UiScale : 1);
                        Vector2 wanted = sizing.sizeStart + delta;
                        sizing.size = new Vector2(Mathf.Clamp(wanted.x, 160, maximum.x), Mathf.Clamp(wanted.y, 100, maximum.y));
                        GUI.changed = true; e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && e.button == 0) { GUIUtility.hotControl = 0; sizing.Save(); e.Use(); }
                    break;
            }
            Fill(handle, new Color(.06f, .16f, .2f, .95f));
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3 - row; col++)
                    Fill(new Rect(handle.xMax - 6 - col * 5, handle.yMax - 6 - row * 5, 3, 3), Cyan);
            GUILayout.Label("Drag the bottom-right corner to resize", note);
            if (MixedInputs && CameraConnected) GUILayout.Label(cameraInput.HandFeedback(Time.realtimeSinceStartup), note);
        }
    }
}
