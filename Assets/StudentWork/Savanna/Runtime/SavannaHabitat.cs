using System;
using System.Linq;
using DigiPhant;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChillinDigiphant.Savanna
{
    public enum HabitatActivity { Drink, Feed, Play }
    [Serializable] public class HabitatZone
    {
        public string title;
        public HabitatActivity activity;
        public Transform anchor;
        public float radius = 3.3f;
        public Renderer marker;
        public Color color;
        public GameObject food;
        [NonSerialized] public float readyAt;
        [NonSerialized] public int uses;
    }

    [DefaultExecutionOrder(100)]
    public sealed class SavannaHabitat : MonoBehaviour
    {
        public DigiPhantController controller;
        public DigiPhantLocomotion locomotion;
        public HabitatZone[] zones;
        public Transform[] waterRipples;
        public ParticleSystem splash;
        public Transform ball;
        public Transform[] obstacles;
        public float[] obstacleRadii;
        public bool keyboardTravel = true;
        public float holdSeconds = .8f;
        public int CurrentZone { get; private set; } = -1;
        public float HoldProgress => hold.Progress;
        public int TotalInteractions { get; private set; }
        readonly InteractionHold hold = new InteractionHold();
        MaterialPropertyBlock markerProperties;
        Vector3 ballStart, ballVelocity;
        float waterUntil, messageUntil;
        string message = "Find a colored circle and explore.";
        bool usedKeyboard, trunkKeyWasHeld;
        bool cameraModeActive;
        GUIStyle titleStyle, bodyStyle, smallStyle;

        void Awake() { markerProperties = new MaterialPropertyBlock(); }
        void Start() { if (ball) ballStart = ball.position; }
        void Update()
        {
            if (controller)
            {
                var preview = controller.GetComponent<DigiPhantCameraPreview>();
                bool cameraMode = controller.inputMode == InputMode.Camera;
                if (preview)
                {
                    preview.showPreview = cameraMode && controller.showControls;
                    if (cameraMode && !cameraModeActive) { preview.autoStartBridge = true; preview.BeginPreview(); }
                    if (!cameraMode && cameraModeActive) preview.StopPreview();
                }
                cameraModeActive = cameraMode;
            }
            var keys = Keyboard.current;
            if (keys == null || !controller || !locomotion) return;
            if (keys.tabKey.wasPressedThisFrame) controller.showControls = !controller.showControls;
            if (keys.rKey.wasPressedThisFrame) ResetHabitat();
            if (controller.inputMode != InputMode.TestSliders) { usedKeyboard = false; trunkKeyWasHeld = false; return; }
            if (keyboardTravel)
            {
                float f = (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0);
                float t = (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
                if (f != 0 || t != 0 || usedKeyboard)
                { locomotion.testForward = f * (keys.leftShiftKey.isPressed ? 1 : .55f); locomotion.testSteering = t; }
                usedKeyboard = f != 0 || t != 0;
            }
            var trunk = controller.controls.FirstOrDefault(c => c.label == "Trunk curl");
            bool held = keys.eKey.isPressed;
            if (trunk != null && (held || trunkKeyWasHeld)) trunk.testValue = held ? 1 : 0;
            trunkKeyWasHeld = held;
        }
        void OnApplicationFocus(bool focus)
        {
            if (focus || !controller || controller.inputMode != InputMode.TestSliders) return;
            locomotion.StopMotion();
            if (trunkKeyWasHeld)
            { var trunk = controller.controls.FirstOrDefault(c => c.label == "Trunk curl"); if (trunk != null) trunk.testValue = 0; }
            usedKeyboard = trunkKeyWasHeld = false;
            hold.Reset();
        }
        void LateUpdate()
        {
            if (!controller || !locomotion || !locomotion.travelRoot) return;
            KeepClearOfTrees();
            var trunk = controller.controls.FirstOrDefault(c => c.label == "Trunk curl");
            float signal = trunk != null ? trunk.testValue : 0;
            bool valid = trunk != null;
            if (controller.inputMode == InputMode.Camera)
            {
                valid = trunk != null && controller.TryReadMovement(trunk.performer, trunk.movement, Time.realtimeSinceStartup, out signal);
                if (valid) signal = Mathf.Clamp(signal * trunk.sensitivity, -1, 1);
            }
            TickInteraction(Time.time, Time.deltaTime, valid, signal);
            TickEffects(Time.time, Time.deltaTime);
        }
        public int FindZone(Vector3 position, float now)
        {
            int selected = -1; float nearest = float.MaxValue;
            for (int i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                if (!zone.anchor || now < zone.readyAt) continue;
                Vector3 d = position - zone.anchor.position; d.y = 0;
                if (d.sqrMagnitude <= zone.radius * zone.radius && d.sqrMagnitude < nearest)
                { selected = i; nearest = d.sqrMagnitude; }
            }
            return selected;
        }
        public void TickInteraction(float now, float dt, bool valid, float signal)
        {
            CurrentZone = FindZone(locomotion.travelRoot.position, now);
            if (hold.Step(CurrentZone, valid, signal, dt, holdSeconds)) Activate(zones[CurrentZone], now);
            for (int i = 0; i < zones.Length; i++)
            {
                var z = zones[i];
                if (z.activity == HabitatActivity.Play && z.marker && ball)
                    z.marker.transform.position = new Vector3(ball.position.x, .05f, ball.position.z);
                if (z.food && now >= z.readyAt) z.food.SetActive(true);
                if (!z.marker) continue;
                var color = z.color * (i == CurrentZone ? 1.2f : .7f); color.a = 1;
                markerProperties.SetColor("_BaseColor", color);
                z.marker.SetPropertyBlock(markerProperties);
            }
        }
        void Activate(HabitatZone zone, float now)
        {
            zone.uses++; TotalInteractions++;
            zone.readyAt = now + (zone.activity == HabitatActivity.Feed ? 8 : 2.5f);
            if (zone.activity == HabitatActivity.Drink)
            { waterUntil = now + 3; if (splash) splash.Play(); message = "A cool drink. Watch the water ripple."; }
            if (zone.activity == HabitatActivity.Feed)
            { if (zone.food) zone.food.SetActive(false); message = "Snack time! Fresh fruit returns in 8 seconds."; }
            if (zone.activity == HabitatActivity.Play && ball)
            {
                Vector3 away = ball.position - locomotion.travelRoot.position; away.y = 0;
                if (away.sqrMagnitude < .01f) away = locomotion.travelRoot.forward;
                ballVelocity = away.normalized * 5.5f;
                message = "A trunk nudge! Follow the ball to play again.";
            }
            messageUntil = now + 4;
        }
        public void TickEffects(float now, float dt)
        {
            if (ball)
            {
                Vector3 move = ballVelocity * Mathf.Min(dt, .1f);
                ball.position += move;
                if (move.sqrMagnitude > .00001f) ball.Rotate(Vector3.Cross(Vector3.up, move.normalized), move.magnitude / .9f * Mathf.Rad2Deg, Space.World);
                ballVelocity = Vector3.MoveTowards(ballVelocity, Vector3.zero, dt * 1.8f);
                Vector3 flat = ball.position; flat.y = 0;
                if (flat.magnitude > 20) { flat = flat.normalized * 20; ball.position = new Vector3(flat.x, ball.position.y, flat.z); ballVelocity = Vector3.zero; }
            }
            if (waterRipples != null)
                for (int i = 0; i < waterRipples.Length; i++)
                {
                    float p = Mathf.Repeat(now * (now < waterUntil ? .7f : .15f) + i / 3f, 1);
                    waterRipples[i].localScale = Vector3.one * Mathf.Lerp(.3f, now < waterUntil ? 3.5f : 1.8f, p);
                }
        }
        void KeepClearOfTrees()
        {
            Vector3 p = locomotion.travelRoot.position;
            for (int i = 0; i < obstacles.Length && i < obstacleRadii.Length; i++)
            {
                if (!obstacles[i]) continue;
                Vector3 d = p - obstacles[i].position; d.y = 0;
                float radius = obstacleRadii[i];
                if (d.sqrMagnitude >= radius * radius) continue;
                if (d.sqrMagnitude < .0001f) d = Vector3.back;
                Vector3 corrected = obstacles[i].position + d.normalized * radius;
                p.x = corrected.x; p.z = corrected.z;
            }
            locomotion.travelRoot.position = p;
        }
        public void ResetHabitat()
        {
            locomotion.ResetPosition(); controller.ResetControls();
            hold.Reset(); CurrentZone = -1; TotalInteractions = 0; ballVelocity = Vector3.zero;
            if (ball) { ball.position = ballStart; ball.rotation = Quaternion.identity; }
            foreach (var z in zones) { z.readyAt = 0; z.uses = 0; if (z.food) z.food.SetActive(true); }
            if (splash) splash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            waterUntil = 0; messageUntil = 0;
        }
        void OnGUI()
        {
            if (!controller) return;
            float scale = Mathf.Clamp(Screen.height / 850f, .65f, 1.6f);
            var previous = GUI.matrix; GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
                smallStyle = new GUIStyle(bodyStyle) { fontSize = 12 };
            }
            float left = controller.showControls ? (Mathf.Min(350, Screen.width * .32f) + 32) / scale : 26;
            Panel(new Rect(left, 24, 294, 90));
            GUI.Label(new Rect(left + 17, 34, 264, 20), "CHILLIN / A PLACE TO WANDER", smallStyle);
            GUI.Label(new Rect(left + 15, 57, 270, 45), "Savanna watering hole", titleStyle);
            float x = Mathf.Max(left, w - 256);
            Panel(new Rect(x, 24, 230, 140));
            GUI.Label(new Rect(x + 16, 36, 208, 25), "EXPLORE AT YOUR OWN PACE", smallStyle);
            for (int i = 0; i < zones.Length; i++)
            {
                GUI.color = zones[i].color; GUI.DrawTexture(new Rect(x + 16, 71 + i * 27, 7, 7), Texture2D.whiteTexture); GUI.color = Color.white;
                GUI.Label(new Rect(x + 33, 62 + i * 27, 183, 27), zones[i].title + (zones[i].uses > 0 ? "   /   visited" : ""), bodyStyle);
            }
            float panelWidth = Mathf.Min(580, w - left - 26);
            Panel(new Rect(left, h - 125, panelWidth, 101));
            string prompt = Time.time < messageUntil ? message : CurrentZone >= 0 ? zones[CurrentZone].title + "  /  Relax, then hold a trunk curl" : "Walk to a colored circle to drink, feed or play.";
            GUI.Label(new Rect(left + 16, h - 115, panelWidth - 32, 42), prompt, bodyStyle);
            GUI.Label(new Rect(left + 16, h - 66, panelWidth - 32, 32), controller.inputMode == InputMode.TestSliders ? "WASD / arrows: move   |   Hold E: trunk   |   Tab: controls   |   R: reset" : "Use your mapped trunk gesture   |   Tab: controls   |   R: reset", smallStyle);
            if (HoldProgress > 0) { GUI.color = new Color(.4f,.85f,.73f); GUI.DrawTexture(new Rect(left, h - 28, panelWidth * HoldProgress, 4), Texture2D.whiteTexture); GUI.color = Color.white; }
            GUI.matrix = previous;
        }
        static void Panel(Rect r) { GUI.color = new Color(.09f,.14f,.12f,.88f); GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color = new Color(.98f,.95f,.85f); }
        void OnDrawGizmosSelected()
        {
            if (zones == null) return;
            foreach (var z in zones) if (z.anchor) { Gizmos.color = z.color; Gizmos.DrawWireSphere(z.anchor.position, z.radius); }
        }
    }
}
