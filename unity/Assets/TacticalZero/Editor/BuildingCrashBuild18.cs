using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
namespace TacticalZero.Editor
{
    public static class BuildingCrashBuild18
    {
        public static void Build()
        {
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                locationPathName="Builds/CrashReplay18/TacticalZero.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Crash replay build failed");
        }
    }
}
