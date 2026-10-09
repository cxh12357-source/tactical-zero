using UnityEngine;

namespace TacticalZero
{
    public static class DisplayMode
    {
        public static void Toggle(){SaveService.Settings.fullscreen=!SaveService.Settings.fullscreen;Apply();SaveService.Save();}
        public static void Apply()
        {
            if(Application.isEditor||Application.isMobilePlatform)return;
            var settings=SaveService.Settings;
            var mode=settings.fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;
            if(Screen.fullScreenMode==mode)return;
            int width=settings.fullscreen?Display.main.systemWidth:Mathf.Min(1600,Display.main.systemWidth);
            int height=settings.fullscreen?Display.main.systemHeight:Mathf.Min(900,Display.main.systemHeight);
            if(width<640||height<480){width=1600;height=900;}
            Screen.SetResolution(width,height,mode);
        }
    }
}
