using UnityEngine;

namespace TacticalZero
{
    public sealed class ExtractionZone : MonoBehaviour
    {
        [SerializeField]
        private float radius = 3;
        private int lastSecond = -1;
        public Vector3 Position => transform.position;

        private void Update()
        {
            var game = GameManager.Instance;
            if (!game || !game.IsPlaying || !game.Player)
                return;
            Vector3 delta = game.Player.transform.position - transform.position;
            bool inside = new Vector2(delta.x, delta.z).sqrMagnitude <= radius * radius && Mathf.Abs(delta.y) < 2;
            if (game.Objective.Tick(inside, Time.deltaTime))
            {
                game.EndMatch(true);
                return;
            }

            int second = Mathf.FloorToInt(game.Objective.ExtractProgress);
            if (second != lastSecond && game.Objective.ExtractProgress > 0)
                AudioService.Instance?.Play("extract", transform.position, .65f, false);
            lastSecond = second;
        }
    }
}
