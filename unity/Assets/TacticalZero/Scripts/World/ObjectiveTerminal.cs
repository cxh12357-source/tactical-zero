using UnityEngine;

namespace TacticalZero
{
    public sealed class ObjectiveTerminal : MonoBehaviour, IInteractable
    {
        public string Label => "情报终端 · 获取加密数据";

        public bool CanInteract(PlayerHealth player, out string reason)
        {
            reason = GameManager.Instance.Objective.HasIntel ? "情报已获取，前往撤离点" : "";
            return player && player.Health > 0 && reason.Length == 0;
        }

        public bool Interact(PlayerHealth player)
        {
            if (!CanInteract(player, out _))
                return false;
            GameManager.Instance.Objective.Acquire();
            NoiseBus.Emit(transform.position, 24, NoiseKind.Terminal);
            GameManager.Instance.Stats.supplies += 180;
            return true;
        }
    }
}
