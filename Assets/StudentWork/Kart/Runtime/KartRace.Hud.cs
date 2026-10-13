using DigiPhant;
using UnityEngine;

namespace ChillinDigiphant.Kart
{
    public sealed partial class KartRace
    {
        [System.NonSerialized] GUIStyle title, text, note, button, numeral, toggle;
        Vector2 panelScroll;
        bool showAdvanced, showSignals;
        static readonly Color Ink = new Color(.035f, .045f, .11f, .96f), Cyan = new Color(.28f, 1, .87f), Gold = new Color(1, .8f, .28f);
        void OnEnable() { title = text = note = button = numeral = toggle = null;if(controller)AttachCameraInput(); }
        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
            text = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            note = new GUIStyle(text) { fontSize = 12 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 13, wordWrap = true, padding = new RectOffset(6, 6, 7, 7) };
            numeral = new GUIStyle(title) { fontSize = 46, alignment = TextAnchor.MiddleCenter };
            toggle = new GUIStyle(GUI.skin.toggle) { fontSize = 12, wordWrap = true };
            toggle.normal.textColor = toggle.onNormal.textColor = Color.white;
            foreach (var s in new[] { title, text, note, numeral }) s.normal.textColor = Color.white;
        }
        static void Fill(Rect r, Color c) { Color old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        void OnGUI()
        {
            Styles(); float scale = UiScale; Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale, side = SidebarPixels / scale;
            if (showMediaPipePanel)
            {
                Fill(new Rect(0, 0, side, h), Ink); Fill(new Rect(side - 2, 0, 2, h), Cyan);
                GUILayout.BeginArea(new Rect(14, 14, side - 28, h - 28)); DrawControls(side - 28, ref panelScroll, true); GUILayout.EndArea();
            }
            float x = side + 20, available = w - x - 20;
            Fill(new Rect(x, 18, available, 70), Ink);
            GUI.Label(new Rect(x + 15, 24, available * .48f, 31), "DIGIPHANT KART", title);
            GUI.Label(new Rect(x + 15, 56, available * .48f, 23), "RAINBOW SAVANNAH  /  " + Phase.ToString().ToUpperInvariant(), note);
            float right = x + available;
            GUI.Label(new Rect(right - 250, 29, 110, 26), "LAP " + Mathf.Min(totalLaps, player.CompletedLaps + 1) + " / " + totalLaps, text);
            GUI.Label(new Rect(right - 250, 54, 110, 25), "PLACE " + Position + " / " + racers.Length, note);
            GUI.Label(new Rect(right - 130, 29, 120, 25), Mathf.FloorToInt(Elapsed / 60).ToString("00") + ":" + (Elapsed % 60).ToString("00.00"), text);
            GUI.Label(new Rect(right - 130, 54, 120, 24), Mathf.Abs(player.Speed * 3.6f).ToString("0") + " KM/H", note);
            Fill(new Rect(x, h - 94, 300, 72), Ink);
            GUI.Label(new Rect(x + 14, h - 84, 170, 26), "ORANGES  " + player.Ammo + " / " + KartVehicle.Capacity, text);
            DrawOrangeStorage(new Vector2(x + 184, h - 87));
            GUI.Label(new Rect(x + 14, h - 53, 280, 24), CameraMode ? "P" + ActionPerformer + (MixedInputs?" pinch / release: throw":" LEFT hand: throw") : "SPACE: throw    E: jump    C: duck", note);
            if (GUI.Button(new Rect(right - 154, h - 54, 154, 32), "CONTROLS  [TAB]", button)) showMediaPipePanel = !showMediaPipePanel;
            DrawMap(new Rect(right - 160, 108, 160, 166));
            DrawOrangeReadyIndicator(scale);
            DrawAudioFeedback(x, available, h);
            var obstacle=NextObstacle;
            string effect = player.Recovering ? "REJOINING / GO IN A MOMENT" : player.Jumping ? "JUMPING" : player.Airborne ? "OFF THE EDGE / RETURNING TO CHECKPOINT" : player.StunRemaining > 0 ? player.BananaHit ? "BANANA SLIP / DIZZY!" : "ORANGE HIT / SPEED RECOVERING" : player.Crouching ? "DUCKING / LOW PROFILE" : obstacle ? obstacle.Prompt : player.BoostRemaining > 0 ? "RAINBOW BOOST" : "";
            if(effect.Length>0)
            {
                float ew=Mathf.Min(available-280,370), ex=x+(available-ew)/2;
                Fill(new Rect(ex,h-142,ew,44),Ink);
                GUI.Label(new Rect(ex+10,h-135,ew-20,30),effect,note);
                float amount=player.StunRemaining>0?player.StunRemaining/1.15f:player.BoostRemaining>0?player.BoostRemaining/1.8f:1;
                Fill(new Rect(ex,h-101,ew*amount,3),player.StunRemaining>0?Gold:Cyan);
            }
            if (!string.IsNullOrEmpty(Message))
            {
                float mw = Mathf.Min(available - 10, 560);
                Fill(new Rect(x + (available - mw) / 2, 104, mw, 42), Ink);
                GUI.Label(new Rect(x + (available - mw) / 2 + 10, 114, mw - 20, 26), Message, text);
            }
            if (TrackingBlocked || Paused || Phase == RacePhase.Countdown || Phase == RacePhase.Finished)
            {
                float cw = Mathf.Min(available - 20, 440), cx = x + (available - cw) / 2, cy = h * .37f;
                Fill(new Rect(cx, cy, cw, 176), Ink); Fill(new Rect(cx, cy, cw, 3), Cyan);
                string headline = TrackingBlocked ? CameraStartPending ? "CAMERA SETUP" : CameraConnected ? "CAMERA TEAM" : "PLAY WITH CAMERA" : Paused ? "PAUSED" : Phase == RacePhase.Countdown ? Mathf.CeilToInt(Countdown).ToString() : "FINISH  /  " + Position + " of " + racers.Length;
                GUI.Label(new Rect(cx + 16, cy + 15, cw - 32, 60), headline, Phase == RacePhase.Countdown && !TrackingBlocked && !Paused ? numeral : title);
                GUI.Label(new Rect(cx + 20, cy + 77, cw - 40, 50), TrackingBlocked ? CameraStartStatus : Paused ? "Press P or Resume to keep playing." : Phase == RacePhase.Finished ? "Three laps complete. Start again for another race." : CameraMode ? "Accelerate, steer, and throw together." : "WASD to drive / Space to throw.", text);
                if (TrackingBlocked)
                {
                    if (Phase == RacePhase.Practice || Phase == RacePhase.Finished || CameraStartPending)
                    {
                        if (GUI.Button(new Rect(cx + 20, cy + 132, cw - 40, 30), CameraStartLabel, button))
                        { if (CameraStartPending) CancelCameraStart(); else StartWithCamera(); }
                    }
                    else if (GUI.Button(new Rect(cx + 20, cy + 132, cw - 40, 30), "SHOW CAMERA CONTROLS", button)) showMediaPipePanel = true;
                }
                else if (Phase == RacePhase.Finished && GUI.Button(new Rect(cx + 20, cy + 132, cw - 40, 30), "RACE AGAIN  [ENTER]", button)) StartRace();
                else if (Paused && GUI.Button(new Rect(cx + 20, cy + 132, cw - 40, 30), "RESUME  [P]", button)) TogglePause();
            }
            else if (Phase == RacePhase.Practice && GUI.Button(new Rect(x + (available - 210) / 2, h - 65, 210, 40), "START RACE  [ENTER]", button)) StartRace();
            GUI.matrix = old;
        }
        public void DrawControls(float width, ref Vector2 scroll, bool inGame = false)
        {
            Styles(); scroll = GUILayout.BeginScrollView(scroll, false, true);
            GUILayout.BeginVertical(GUILayout.Width(Mathf.Max(120, width - 30)));
            GUILayout.Label("CONTROLS", title);
            int mode = CameraMode ? 0 : 1;
            int choice = GUILayout.Toolbar(mode, new[] { "Camera", "Keys" }, button, GUILayout.Height(38));
            if (choice != mode)
            {
                if (choice == 0) SelectCamera(); else SelectKeys();
            }
            GUILayout.Space(8);
            if (CameraMode) DrawTeamControls(width, inGame);
            else
            {
                GUILayout.Label("SOLO PLAY", text);
                GUILayout.Label("Your keyboard controls the whole kart. No camera or calibration needed.", note);
                GUILayout.Space(8);
                GUILayout.Label("W / S  or  arrows    Drive / reverse\nA / D  or  arrows    Steer\nShift + forward    Full throttle\nSpace    Trunk throw\nE / J    Jump\nHold C / Ctrl    Duck", text);
#if UNITY_EDITOR
                GUILayout.Space(6);
                if (GUILayout.Button("PLAY WITH KEYS", button, GUILayout.Height(34))) FocusKeyboardPlay();
                GUILayout.Label("Keyboard input goes to the Game view.", note);
#endif
            }
            GUILayout.Space(10);
            GUILayout.Label("Store up to 3 oranges from item boxes. Each throw uses one. Later laps add logs, low canopies and banana peels: jump over peels to avoid a dizzy slowdown.", note);
            if (!CameraMode && GUILayout.Button(Phase == RacePhase.Practice ? "START RACE  [ENTER]" : "RESTART RACE  [ENTER]", button, GUILayout.Height(38))) StartRace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Paused ? "Resume [P]" : "Pause [P]", button)) TogglePause();
            if (GUILayout.Button("Reset [R]", button)) ResetRace();
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Recover to checkpoint [Backspace]", button)) player.Recover();
            if (Audio)
            {
                GUILayout.Space(10);
                GUILayout.Label("AUDIO", text);
                Audio.muted = GUILayout.Toggle(Audio.muted, "Mute all sound", toggle);
                GUILayout.Label("Music", note); Audio.musicVolume = GUILayout.HorizontalSlider(Audio.musicVolume, 0, 1);
                GUILayout.Label("Action sounds", note); Audio.effectsVolume = GUILayout.HorizontalSlider(Audio.effectsVolume, 0, 1);
            }
            if (CameraMode)
            {
                GUILayout.Space(10);
                showAdvanced = GUILayout.Toggle(showAdvanced, "Camera settings & diagnostics", toggle);
                if (showAdvanced) DrawAdvancedCameraControls(width);
            }
            // Returning focus after any dock button also covers Pause, Reset and Start Race.
            if (!CameraMode && Event.current.type == EventType.Used && Event.current.rawType == EventType.MouseUp) FocusKeyboardPlay();
            GUILayout.EndVertical(); GUILayout.EndScrollView();
        }
        void DrawAudioFeedback(float x, float available, float height)
        {
            if (!Audio) return;
            if (Audio.BannerRemaining > 0)
            {
                float bw = Mathf.Min(available - 20, 390), bx = x + (available - bw) / 2;
                float alpha = Mathf.Clamp01(Audio.BannerRemaining * 2);
                Fill(new Rect(bx, 162, bw, 70), new Color(Ink.r, Ink.g, Ink.b, alpha * .96f));
                Fill(new Rect(bx, 162, bw, 4), Gold * new Color(1, 1, 1, alpha));
                var style = new GUIStyle(title) { alignment = TextAnchor.MiddleCenter, fontSize = 28 };
                style.normal.textColor = new Color(1, .85f, .35f, alpha);
                GUI.Label(new Rect(bx + 8, 174, bw - 16, 40), Audio.LapBanner, style);
            }
            if (Audio.CaptionRemaining > 0)
            {
                float cw = Mathf.Min(available - 20, 240), cx = x + (available - cw) / 2;
                Fill(new Rect(cx, height - 181, cw, 28), Ink);
                var style = new GUIStyle(note) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(cx + 5, height - 177, cw - 10, 22), Audio.Caption, style);
            }
        }
        void DrawTeamControls(float width, bool inGame)
        {
            GUILayout.Label("TEAM PLAY / ONE CAMERA", text);
            DrawCameraStartButton(inGame);
            int count = GUILayout.Toolbar(Mathf.Clamp(controller.performerCount - 2, 0, 2), new[] { "2 players", "3 players", "4 players" }, button) + 2;
            if (count != controller.performerCount) UsePerformers(count);
            if (count == 3) GUILayout.Label("Left to right: P1 body / P2 shoulders / P3 hand", note);
            var preview = Preview;
            if (preview)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(CameraConnected ? "Restart camera" : "START CAMERA", button, GUILayout.Height(34))) ConnectCamera();
                if (CameraConnected && GUILayout.Button("Stop", button, GUILayout.Width(50), GUILayout.Height(34))) DisconnectCamera();
                GUILayout.EndHorizontal();
                DrawResizablePreview(preview, width, inGame);
            }
            if (controller.CalibrationPending)
            {
                GUILayout.Label("Hold neutral: " + controller.CalibrationSecondsRemaining + " seconds", text);
                if (GUILayout.Button("Cancel", button)) CancelCameraStart();
            }
            else
            {
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && CameraConnected;
                if (GUILayout.Button(controller.IsCalibrated ? "RECALIBRATE / 10 sec" : "SET NEUTRAL / 10 sec", button, GUILayout.Height(34))) controller.BeginCalibrationCountdown(Time.realtimeSinceStartup);
                GUI.enabled = previousEnabled;
            }
            GUILayout.Label(MixedInputs ? "P1 BODY: left hand drives / right brakes\nP" + SteeringPerformer + " SHOULDERS: " + ShoulderSteeringHelp + "\nP" + ActionPerformer + " HAND: " + (count == 3 ? "one hand only / " : "right hand / ") + "pinch + release throws" :
                "P1: left hand drives / right brakes\nP" + SteeringPerformer + ": raise left / right hand to steer\nP" + ActionPerformer + ": hold left hand to throw", note);
            GUILayout.Label(MixedInputs ? "P" + ActionPerformer + ": thumbs up (other fingers curled) to jump; relax fingers to rearm." + "\nP" + ActionPerformer + ": hold a fist to crouch; open to stand." : literalBodyActions ? "P" + BodyPerformer + ": jump and duck with your body. Keep feet visible." : "P" + ActionPerformer + ": raise right hand to jump / lean right to duck.", note);
        }
        void DrawAdvancedCameraControls(float width)
        {
            var preview = Preview;
            if (preview)
            {
                GUILayout.BeginHorizontal(); GUILayout.Label("Camera " + preview.cameraIndex, text);
                GUI.enabled = !CameraConnected;
                if (GUILayout.Button("-", button, GUILayout.Width(30))) preview.cameraIndex = Mathf.Max(0, preview.cameraIndex - 1);
                if (GUILayout.Button("+", button, GUILayout.Width(30))) preview.cameraIndex = Mathf.Min(8, preview.cameraIndex + 1);
                GUI.enabled = true; GUILayout.EndHorizontal();
                bool mirror = GUILayout.Toggle(preview.mirrorPreview, "Mirror preview", toggle);
                if (mirror != preview.mirrorPreview) preview.SetMirrorPreview(mirror);
                GUILayout.Label(preview.Status, note);
            }
            int layout = GUILayout.Toolbar((int)cameraLayout, new[] { "Simple body", "Body + shoulders + hand" }, button);
            if (layout != (int)cameraLayout) SetCameraLayout((KartCameraLayout)layout);
            int frame = GUILayout.Toolbar(controller.upperBodyOnly ? 1 : 0, new[] { "Standing", "Seated" }, button);
            if ((frame == 1) != controller.upperBodyOnly) { controller.SetUpperBodyOnly(frame == 1); SetLiteralActions(frame == 0); }
            DrawCameraOptions();
            if (GUILayout.Button("Reassign players", button)) controller.ReassignPeople();
            GUILayout.Label(ActionStatus, note);
            showSignals = GUILayout.Toggle(showSignals, "Live movement meters", toggle);
            if (showSignals)
            {
                Meter("Forward / LEFT", 1, Movement.LeftHandHeight);
                Meter("Reverse / RIGHT", 1, Movement.RightHandHeight);
                if(MixedInputs){GUILayout.Label("Shoulder steering: "+cameraInput.Steering.ToString("0.00")+" / pinch gap: "+cameraInput.Pinch.ToString("0.00"),note);}
                else {Meter("Steer / LEFT",SteeringPerformer,Movement.LeftHandHeight);Meter("Steer / RIGHT",SteeringPerformer,Movement.RightHandHeight);Meter("Throw / LEFT",ActionPerformer,Movement.LeftHandHeight);}
                if(!MixedInputs && literalBodyActions)GUILayout.Label("Body rise: "+cameraInput.BodyRise.ToString("0.00")+" / duck depth: "+cameraInput.BodyDrop.ToString("0.00"),note);
                else if(!MixedInputs) {Meter("Jump / RIGHT",ActionPerformer,Movement.RightHandHeight);Meter("Duck / LEAN RIGHT",ActionPerformer,Movement.Lean);}
            }
            var recording = controller.GetComponent<DigiPhantRecording>(); if (recording) { GUILayout.Space(8); recording.DrawControls(width - 22); }
        }
        void Meter(string label, int p, Movement movement)
        {
            bool valid = controller.TryReadMovement(p, movement, Time.realtimeSinceStartup, out float v);
            GUILayout.Label("P" + p + " " + label + ": " + (valid ? v.ToString("+0.00;-0.00;0.00") : "waiting"), note);
        }
        void DrawZones(Rect box, Texture frame)
        {
            if (frame)
            {
                float fit = Mathf.Min(box.width / frame.width, box.height / frame.height);
                box = new Rect(box.center.x - frame.width * fit / 2, box.center.y - frame.height * fit / 2, frame.width * fit, frame.height * fit);
            }
            Fill(new Rect(box.x, box.y, box.width, 35), new Color(.7f, .05f, .13f, .95f));
            for (int z = 0; z < 3; z++)
            {
                float x = box.x + box.width * z / 3, w = box.width / 3;
                GUI.Label(new Rect(x + 4, box.y, w - 8, 18), "P" + (z + 1), note);
                GUI.Label(new Rect(x + 4, box.y + 16, w - 8, 18), MixedInputs ? z==0?"BODY":z==1?"SHRUG":"HAND" : "L        R", note);
                if (z > 0) Fill(new Rect(x - 1, box.y, 2, box.height), Color.red);
            }
        }
        void DrawMap(Rect r)
        {
            Fill(r, Ink); GUI.Label(new Rect(r.x + 10, r.y + 6, r.width - 20, 20), "CIRCUIT / NEXT GATE", note);
            Vector3 low = track.points[0], high = low;
            foreach (var p in track.points) { low = Vector3.Min(low, p); high = Vector3.Max(high, p); }
            Vector3 center = (low + high) * .5f;
            float fit = Mathf.Min((r.width - 26) / Mathf.Max(1, high.x - low.x), (r.height - 48) / Mathf.Max(1, high.z - low.z));
            Vector2 Map(Vector3 p) => new Vector2(r.center.x + (p.x - center.x) * fit, r.y + 34 + (r.height - 48) * .5f - (p.z - center.z) * fit);
            for (int i = 0; i < track.Count; i++)
            {
                Vector2 a = Map(track.points[i]), b = Map(track.points[(i + 1) % track.Count]);
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
                for (int j = 0; j <= steps; j++) { Vector2 p = Vector2.Lerp(a, b, j / (float)steps); Fill(new Rect(p.x - 1, p.y - 1, 2, 2), new Color(.55f, .48f, .83f)); }
            }
            Vector2 gate = Map(track.points[track.gates[player.NextGate]]); Fill(new Rect(gate.x - 4, gate.y - 4, 8, 8), Gold);
            foreach (var kart in racers) { Vector2 p = Map(track.transform.InverseTransformPoint(kart.transform.position)); Fill(new Rect(p.x - 3, p.y - 3, 6, 6), kart.isPlayer ? Cyan : new Color(1, .35f, .55f)); }
        }
    }
}
