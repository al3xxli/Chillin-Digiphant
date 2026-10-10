using UnityEngine;

namespace ChillinDigiphant.Kart
{
    public sealed partial class KartRace
    {
        Transform orangeIndicatorHead;
        Texture2D orangeReadyIcon;

        void DrawOrangeStorage(Vector2 position)
        {
            if (!orangeReadyIcon) orangeReadyIcon = CreateOrangeReadyIcon();
            Color previous = GUI.color;
            for (int slot = 0; slot < KartVehicle.Capacity; slot++)
            {
                GUI.color = slot < player.Ammo ? Color.white : new Color(.4f, .4f, .4f, .4f);
                GUI.DrawTexture(new Rect(position.x + slot * 34, position.y, 28, 32), orangeReadyIcon, ScaleMode.ScaleToFit, true);
            }
            GUI.color = previous;
        }

        void DrawOrangeReadyIndicator(float scale)
        {
            if (!player || !player.isPlayer || !player.CanThrow) return;
            var camera = locomotion.followCamera;
            if (!camera) return;
            Vector3 anchor = orangeIndicatorHead ? orangeIndicatorHead.position + Vector3.up * 1.25f : player.transform.position + Vector3.up * 4;
            Vector3 screen = camera.WorldToScreenPoint(anchor);
            if (screen.z <= camera.nearClipPlane || !camera.pixelRect.Contains(new Vector2(screen.x, screen.y))) return;
            const float width = 32, height = 36;
            float x = screen.x / scale, y = (Screen.height - screen.y) / scale - Mathf.Sin(Time.time * 2.8f) * 2;
            var rect = new Rect(x - width / 2, y - height / 2, width, height);
            if (rect.xMin < SidebarPixels / scale + 8 || rect.yMin < 96) return;
            if (!orangeReadyIcon) orangeReadyIcon = CreateOrangeReadyIcon();
            GUI.DrawTexture(rect, orangeReadyIcon, ScaleMode.ScaleToFit, true);
        }

        // A small code-drawn fruit silhouette: orange, highlight, stem and green leaf.
        static Texture2D CreateOrangeReadyIcon()
        {
            const int width = 64, height = 72;
            var pixels = new Color[width * height];
            Color outline = new Color(.18f, .09f, .025f), fruit = new Color(1, .48f, .04f);
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                Color color = Color.clear;
                // Supersampling keeps the compact floating icon smooth at different Game view sizes.
                for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++)
                {
                    float px = x + (sx + .5f) / 2, py = y + (sy + .5f) / 2;
                    float radius = Vector2.Distance(new Vector2(px, py), new Vector2(31, 28));
                    Color sample = Color.clear;
                    if (px > 29 && px < 34 && py > 46 && py < 57) sample = outline;
                    if (radius < 24) sample = outline;
                    if (radius < 21.5f) sample = Color.Lerp(new Color(.94f, .29f, .025f), fruit, Mathf.Clamp01(py / 40));
                    float shine = Mathf.Pow((px - 22) / 3.2f, 2) + Mathf.Pow((py - 35) / 6.5f, 2);
                    if (shine < 1) sample = new Color(1, .79f, .32f);
                    float lx = (px - 41) * .87f + (py - 57) * .5f, ly = -(px - 41) * .5f + (py - 57) * .87f;
                    float leaf = lx * lx / 121 + ly * ly / 25;
                    if (leaf < 1) sample = new Color(.08f, .24f, .055f);
                    if (leaf < .6f) sample = new Color(.43f, .81f, .18f);
                    color += sample * .25f;
                }
                pixels[y * width + x] = color.a > 0 ? new Color(color.r / color.a, color.g / color.a, color.b / color.a, color.a) : Color.clear;
            }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "Orange ready icon", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels); texture.Apply(false, true); return texture;
        }
    }
}
