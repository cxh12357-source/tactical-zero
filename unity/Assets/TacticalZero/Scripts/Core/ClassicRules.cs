using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalZero
{
    [Serializable] public class ClassicRole { public string id, name, tag, color, skill, detail; public float cooldown, duration; }
    [Serializable] public class ClassicGun { public string id, label, desc, kind, tier, color; public int mag, reserve, pellets, pierce; public float damage, interval, reload, range, spread, recoil, head, blast, fan, speed; public bool limited; }
    [Serializable] public class ClassicDifficulty { public string id, label, description; public int count; public float error, damage, turn; public float[] reaction, burst, rest; public string[] guns; }
    [Serializable] public class ClassicRect { public int x,y,w,h; }
    [Serializable] public class ClassicPOI { public string name,type; public int x,y; }
    [Serializable] public class ClassicMap
    {
        public string id,name,code,description,wall,metal,grass;
        public int[] floor;
        public ClassicRect[] walls,crates,brush,water;
        public ClassicPOI[] pois;
        public string Scene => "Classic_" + id;
        public bool Blocked(int x,int y)
        {
            if(x<1||y<1||x>=27||y>=19) return true;
            foreach(var r in walls) if(x>=r.x&&x<r.x+r.w&&y>=r.y&&y<r.y+r.h) return true;
            foreach(var r in crates) if(x>=r.x&&x<r.x+r.w&&y>=r.y&&y<r.y+r.h) return true;
            return false;
        }
        public static Vector3 Position(float x,float y,float height=0) => new Vector3((x-14)*3,height,(10-y)*3);
    }
    [Serializable] public class ClassicData { public ClassicRole[] roles; public ClassicGun[] weapons; public ClassicDifficulty[] difficulties; public ClassicMap[] maps; }
    public static class ClassicRules
    {
        static ClassicData data;
        public static ClassicData Data => data ??= JsonUtility.FromJson<ClassicData>(Resources.Load<TextAsset>("Classic/Rules").text);
        public static bool InMatch => SceneManager.GetActiveScene().name.StartsWith("Classic_");
        public static int RoleIndex { get => 0; set { /* Old role selections are ignored; unrelated settings are preserved. */ } }
        public static int MapIndex { get => Mathf.Clamp(PlayerPrefs.GetInt("TZ3D.Map",0),0,Data.maps.Length-1); set { PlayerPrefs.SetInt("TZ3D.Map",Mathf.Clamp(value,0,Data.maps.Length-1)); PlayerPrefs.Save(); } }
        public static int DifficultyIndex { get => Mathf.Clamp(PlayerPrefs.GetInt("TZ3D.Difficulty",1),0,Data.difficulties.Length-1); set { PlayerPrefs.SetInt("TZ3D.Difficulty",Mathf.Clamp(value,0,Data.difficulties.Length-1)); PlayerPrefs.Save(); } }
        public static bool AutoAim { get => PlayerPrefs.GetInt("TZ3D.AutoAim",1)==1; set { PlayerPrefs.SetInt("TZ3D.AutoAim",value?1:0); PlayerPrefs.Save(); } }
        public static ClassicRole Role => Data.roles[RoleIndex];
        public static ClassicMap Map => Data.maps[MapIndex];
        public static ClassicDifficulty Difficulty => Data.difficulties[DifficultyIndex];
        public static ClassicGun Gun(string id) => Array.Find(Data.weapons,x=>x.id==id);
        public static WeaponDefinition Weapon(string id) => Resources.Load<WeaponDefinition>("Classic/Weapons/"+id);
        public static Color ColorOf(string hex) => ColorUtility.TryParseHtmlString(hex,out var color)?color:Color.white;
    }
}
