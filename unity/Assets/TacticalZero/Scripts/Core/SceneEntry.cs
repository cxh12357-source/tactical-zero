using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalZero
{
    [DefaultExecutionOrder(-500)]
    public sealed class SceneEntry : MonoBehaviour
    {
        private void Awake()
        {
            GameManager.Ensure().EnterScene(SceneManager.GetActiveScene().name);
        }
    }
}
