using UnityEngine;

namespace TacticalZero
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Start()
        {
            GameManager.Ensure().MainMenu();
        }
    }
}
