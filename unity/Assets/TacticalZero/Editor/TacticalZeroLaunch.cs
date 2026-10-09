using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TacticalZero.Editor
{
    public static class TacticalZeroLaunch
    {
        [MenuItem("Tactical Zero/Open and Play Game")]
        public static void OpenAndPlay()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            ClassicProjectSetup.Setup();
            BuildingSweepBuild.EnsureScenes();
            TacticalZeroValidation.Validate();
            EditorSceneManager.OpenScene(TacticalZeroProjectSetup.Root + "/Scenes/Boot.unity");
            EditorApplication.delayCall += () =>
            {
                EditorApplication.isPlaying = true;
                Debug.Log("Tactical Zero: requested Play Mode from Boot scene.");
            };
        }
    }
}
