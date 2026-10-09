using UnityEngine;

namespace TacticalZero
{
    public enum SightStyle { Iron, Reflex, Holographic, Scope, Digital }
    public readonly struct WeaponSight
    {
        public readonly SightStyle Style;
        public readonly float Fov, Height;
        public readonly string Label;
        public bool Magnified=>Style==SightStyle.Scope||Style==SightStyle.Digital;
        WeaponSight(SightStyle style,float fov,float height,string label){Style=style;Fov=fov;Height=height;Label=label;}
        public static WeaponSight For(string id)
        {
            switch(id)
            {
                case "M700":case "AWP":return new WeaponSight(SightStyle.Scope,15,.16f,"8× · MIL-DOT");
                case "DMR":return new WeaponSight(SightStyle.Scope,25,.16f,"4× · BDC");
                case "RAIL":return new WeaponSight(SightStyle.Digital,20,.16f,"6× · DIGITAL");
                case "P90":return new WeaponSight(SightStyle.Reflex,54,.17f,"REFLEX · 1×");
                case "M4A1":case "VECTOR":return new WeaponSight(SightStyle.Reflex,54,.14f,"REFLEX · 1×");
                case "MG3":return new WeaponSight(SightStyle.Iron,58,.117f,"IRON SIGHTS");
                case "M1014":return new WeaponSight(SightStyle.Iron,58,.095f,"IRON SIGHTS");
                case "CYCLONE":case "NOVA":return new WeaponSight(SightStyle.Holographic,54,.14f,"HOLO · 1×");
                default:return new WeaponSight(SightStyle.Iron,58,id=="USP"?.084f:.13f,"IRON SIGHTS");
            }
        }
    }
}
