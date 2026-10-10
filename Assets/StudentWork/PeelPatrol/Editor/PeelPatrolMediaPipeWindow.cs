using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChillinDigiphant.PeelPatrol.Editor
{
    public sealed class PeelPatrolMediaPipeWindow : EditorWindow
    {
        Vector2 scroll;
        [MenuItem("DigiPhant/MediaPipe Controls")]
        public static void Open()
        {
            var inspector=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            var window=GetWindow<PeelPatrolMediaPipeWindow>("MediaPipe",true,inspector);
            window.minSize=new Vector2(310,400);window.Show();
        }
        [InitializeOnLoadMethod]
        static void RegisterRequest() { EditorApplication.update-=CheckRequest;EditorApplication.update+=CheckRequest; }
        static void CheckRequest()
        {
            if(EditorApplication.isCompiling || !File.Exists(".tools/show-mediapipe-panel"))return;
            File.Delete(".tools/show-mediapipe-panel");Open();
        }
        void OnInspectorUpdate() { Repaint(); }
        void OnGUI()
        {
            var kart=UnityEngine.Object.FindFirstObjectByType<ChillinDigiphant.Kart.KartRace>();
            if(kart)
            {
                if(!EditorApplication.isPlaying)
                {
                    EditorGUILayout.HelpBox("Press Play, then choose Camera for team play or Keys for solo play. Keys works without a camera or calibration.",MessageType.Info);
                    if(GUILayout.Button("ENTER PLAY MODE",GUILayout.Height(36))) { EditorApplication.ExecuteMenuItem("Window/General/Game");EditorApplication.EnterPlaymode(); }
                }
                else
                {
                    var oldColor=GUI.color;GUI.color=new Color(.035f,.045f,.11f);GUI.DrawTexture(new Rect(0,0,position.width,position.height),Texture2D.whiteTexture);GUI.color=Color.white;
                    GUILayout.BeginArea(new Rect(12,12,position.width-24,position.height-24));kart.DrawControls(position.width-24,ref scroll);GUILayout.EndArea();GUI.color=oldColor;
                }
                return;
            }
            var game=UnityEngine.Object.FindFirstObjectByType<PeelPatrolGame>();
            if(!game)
            {
                EditorGUILayout.HelpBox("Open the Peel-Patrol scene to use its MediaPipe controls.",MessageType.Info);return;
            }
            if(!EditorApplication.isPlaying || !game.ControlPanel)
            {
                GUILayout.Label("MediaPipe / DigiPhant",EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Press Play, then Start Camera here or in the Game sidebar. Select Solo or your group, set a neutral pose, and practice before starting waves.",MessageType.Info);
                if(GUILayout.Button("ENTER PLAY MODE",GUILayout.Height(36)))
                {
                    EditorApplication.ExecuteMenuItem("Window/General/Game");
                    EditorApplication.EnterPlaymode();
                }
                GUILayout.Label("Uses the instructor's local tracking bridge and live skeleton preview.",EditorStyles.wordWrappedLabel);return;
            }
            var color=GUI.color;GUI.color=new Color(.065f,.105f,.09f);GUI.DrawTexture(new Rect(0,0,position.width,position.height),Texture2D.whiteTexture);GUI.color=Color.white;
            GUILayout.BeginArea(new Rect(12,12,position.width-24,position.height-24));
            game.ControlPanel.DrawContents(position.width-24,ref scroll);
            GUILayout.EndArea();GUI.color=color;
        }
    }
}
