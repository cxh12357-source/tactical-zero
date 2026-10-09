using UnityEngine;

namespace TacticalZero
{
    public enum PickupKind
    {
        Ammo,
        Medical,
        Armor,
        Weapon,
        Supplies
    }

    public sealed class PickupInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private PickupKind kind;
        private bool taken;
        public string Label => kind == PickupKind.Ammo ? "弹药箱" : kind == PickupKind.Medical ? "医疗包" : kind == PickupKind.Armor ? "护甲板" : kind == PickupKind.Weapon ? "霰弹枪补给架" : "物资箱";

        public void Configure(PickupKind type)
        {
            kind = type;
        }

        public bool CanInteract(PlayerHealth player, out string reason)
        {
            reason = "";
            if (taken || !player || player.Health <= 0)
            {
                reason = "无法拾取";
                return false;
            }

            if (kind == PickupKind.Medical && player.Health >= 100)
                reason = "生命已满";
            if (kind == PickupKind.Armor && player.Armor >= 100)
                reason = "护甲已满";
            if (kind == PickupKind.Ammo && !player.GetComponent<WeaponController>().NeedsAmmo)
                reason = "备弹已满";
            return reason.Length == 0;
        }

        public bool Interact(PlayerHealth player)
        {
            if (!CanInteract(player, out _))
                return false;
            switch (kind)
            {
                case PickupKind.Ammo:
                    player.GetComponent<WeaponController>().RefillAll();
                    break;
                case PickupKind.Medical:
                    player.Heal(40);
                    break;
                case PickupKind.Armor:
                    player.AddArmor(50);
                    break;
                case PickupKind.Weapon:
                    var weapons = player.GetComponent<WeaponController>();
                    weapons.RefillAll();
                    weapons.Switch(2);
                    break;
            }

            GameManager.Instance.Stats.supplies += 25;
            taken = true;
            gameObject.SetActive(false);
            return true;
        }
    }
}
