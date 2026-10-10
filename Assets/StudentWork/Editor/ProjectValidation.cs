using System;
using System.Linq;
using DigiPhant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChillinDigiphant.Editor
{
    public static class ProjectValidation
    {
        public const string ScenePath = "Assets/StudentWork/Scenes/Chillin-Digiphant.unity";

        public static void Validate()
        {
            DigiPhant.Editor.DigiPhantLocomotionValidation.RunAll();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var controller = UnityEngine.Object.FindAnyObjectByType<DigiPhantController>();
            if (!controller || controller.controls.Length != 9 ||
                controller.controls.Any(c => c.bones == null || c.bones.Any(b => !b)))
                throw new Exception("Student scene has missing rig mappings.");
            var locomotion = controller.GetComponent<DigiPhantLocomotion>();
            if (!locomotion || !locomotion.Initialise())
                throw new Exception("Student scene locomotion references are incomplete.");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                        throw new Exception("Missing script on " + transform.name);
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>())
                foreach (var material in renderer.sharedMaterials)
                    if (!material || !material.shader || material.shader.name == "Hidden/InternalErrorShader")
                        throw new Exception("Missing material or shader on " + renderer.name);
            if (UnityEngine.Object.FindObjectsByType<Elephant>().Any(e => e.enabled))
                throw new Exception("Vendor controller must be disabled for gesture control.");
            Debug.Log("CHILLIN_DIGIPHANT_VALIDATION_OK: scene, scripts, rig, locomotion, materials, vendor ownership");
        }

        public static void ConfigureAndValidate()
        {
            Validate();
            var controller = UnityEngine.Object.FindAnyObjectByType<DigiPhantController>();
            if (!controller.GetComponent<DigiPhantCameraPreview>())
                controller.gameObject.AddComponent<DigiPhantCameraPreview>();
            if (!controller.GetComponent<DigiPhantRecording>())
                controller.gameObject.AddComponent<DigiPhantRecording>();
            EditorSceneManager.SaveScene(controller.gameObject.scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("CHILLIN_DIGIPHANT_SETUP_OK: preview and recorder attached; student scene first in build settings");
        }
    }
}
