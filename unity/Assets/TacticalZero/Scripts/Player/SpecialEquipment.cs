using UnityEngine;
namespace TacticalZero
{
    public enum EquipmentKind { Armor, Shield, Stim }
    // Obsolete serialized component retained for old scene/save readers; no inventory or behavior.
    public sealed class SpecialEquipment:MonoBehaviour
    {
        public EquipmentKind Selected => EquipmentKind.Armor;
        public float Shield=>0; public float ShieldSeconds=>0; public float StimSeconds=>0;
        public float SpeedMultiplier=>1;
        public int Count(EquipmentKind kind)=>0;
        public bool Collect(EquipmentKind kind)=>false;
        public bool Use()=>false;
        public void Cycle(){} public void ResetEquipment(){}
        public float Absorb(float amount)=>amount;
        public static string Label(EquipmentKind kind)=>"已停用的旧补给";
        public static string Description(EquipmentKind kind)=>"此旧物品不再提供效果";
        public static Color Tint(EquipmentKind kind)=>Color.gray;
    }
}
