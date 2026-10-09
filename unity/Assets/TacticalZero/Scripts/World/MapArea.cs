using System.Collections.Generic;
using UnityEngine;

namespace TacticalZero
{
    public sealed class MapArea : MonoBehaviour
    {
        public static readonly List<MapArea> Active=new List<MapArea>();
        public string Label;public Vector3 Size=new Vector3(10,3,10);public float Movement=1;public bool Interior;
        public bool Contains(Vector3 point){var p=transform.InverseTransformPoint(point);return Mathf.Abs(p.x)<=Size.x*.5f&&p.y>=-.2f&&p.y<=Size.y&&Mathf.Abs(p.z)<=Size.z*.5f;}
        void OnEnable(){Active.Add(this);}void OnDisable(){Active.Remove(this);}
        public static float SpeedAt(Vector3 point){float speed=1;foreach(var area in Active)if(area&&area.Contains(point))speed=Mathf.Min(speed,area.Movement);return speed;}
        public static string NameAt(Vector3 point){foreach(var area in Active)if(area&&area.Interior&&area.Contains(point))return area.Label;foreach(var area in Active)if(area&&area.Contains(point))return area.Label;return "室外行动区";}
    }

}
