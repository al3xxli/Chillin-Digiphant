using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChillinDigiphant.Kart.Editor
{
    [InitializeOnLoad] public static class KartInputSetup
    {
        static KartInputSetup(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(!EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(".tools/reverse-kart-steering"))
            {
                var game=Object.FindAnyObjectByType<KartRace>();
                if(game)
                {
                    game.invertShoulderSteering=true;
                    File.Delete(".tools/reverse-kart-steering");
                    File.WriteAllText("Logs/Kart/steering-direction.txt","Reversed="+game.invertShoulderSteering+"\n"+game.ShoulderSteeringHelp+"\n");
                }
            }
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(!File.Exists(".tools/install-kart-inputs"))return;
            File.Delete(".tools/install-kart-inputs");Install();
        }
        [MenuItem("DigiPhant/Kart/Use Mixed Camera Roles")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=KartBuilder.ScenePath)throw new System.InvalidOperationException("Open Digiphant-Kart before installing camera roles.");
            string backup=AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Kart-BeforeMixedControls.unity");
            EditorSceneManager.SaveScene(scene,backup,true);
            var game=Object.FindAnyObjectByType<KartRace>();
            game.cameraLayout=KartCameraLayout.MixedTeam;game.literalBodyActions=true;
            game.jumpSensitivity=.10f;game.duckDepth=.32f;game.shoulderTurnRange=.30f;game.invertShoulderSteering=true;
            game.controller.upperBodyOnly=false;
            EditorUtility.SetDirty(game);EditorUtility.SetDirty(game.controller);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/Kart/input-setup.txt","Mixed body / shoulders / hand camera roles saved.\nScene: "+scene.path+"\nBackup: "+backup+"\n");
        }
    }
}
