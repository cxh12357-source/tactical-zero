using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace TacticalZero.Editor
{
    public static class TacticalZeroBuild
    {
        public static void ContactWindows(){PlayerSettings.enableFrameTimingStats=true;Build(BuildTarget.StandaloneWindows64,"Builds/ContactReview/TacticalZero.exe",false);}
        public static void BodycamWindows(){PlayerSettings.enableFrameTimingStats=true;Build(BuildTarget.StandaloneWindows64,"Builds/BodycamReview/TacticalZero.exe",false);}
        public static void RefinedWindows(){Build(BuildTarget.StandaloneWindows64,"Builds/RefinedReview/TacticalZero.exe");}
        public static void FieldWindows(){FieldReview14.Render();Build(BuildTarget.StandaloneWindows64,"Builds/FieldReview/TacticalZero.exe");}
        [MenuItem("Tactical Zero/Build Windows Development")]
        public static void Windows()
        {
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/TacticalZero.exe");
        }

        [MenuItem("Tactical Zero/Build Android Development")]
        public static void Android()
        {
            Build(BuildTarget.Android, "Builds/Android/TacticalZero.apk");
        }

        [MenuItem("Tactical Zero/Export iOS Xcode Project")]
        public static void IOS()
        {
            Build(BuildTarget.iOS, "Builds/iOS");
        }

        private static void Build(BuildTarget target, string path, bool development=true)
        {
            ClassicProjectSetup.Setup();
            BuildingSweepBuild.EnsureScenes();
            TacticalZeroValidation.Validate();
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                throw new InvalidOperationException("Install the Unity Hub build support module for " + target + ", then run the build again.");
            if (target == BuildTarget.Android || target == BuildTarget.iOS)
                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target)), ScriptingImplementation.IL2CPP);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var scenes=new System.Collections.Generic.List<string>{TacticalZeroProjectSetup.Root+"/Scenes/Boot.unity",TacticalZeroProjectSetup.Root+"/Scenes/MainMenu.unity",TacticalZeroProjectSetup.Root+"/Scenes/Outpost01.unity"};
            foreach(var map in ClassicRules.Data.maps)scenes.Add(TacticalZeroProjectSetup.Root+"/Scenes/"+map.Scene+".unity");
            foreach(var map in BuildingCatalog.Maps)scenes.Add(TacticalZeroProjectSetup.Root+"/Scenes/"+map.Scene+".unity");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes.ToArray(), locationPathName = path, target = target, options = development?BuildOptions.Development:BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build failed. See the Unity Console and build log: " + report.summary.result);
        }
    }
}
