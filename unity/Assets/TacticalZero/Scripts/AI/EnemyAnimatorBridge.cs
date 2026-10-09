using UnityEngine;
using UnityEngine.AI;

namespace TacticalZero
{
    public sealed class EnemyAnimatorBridge : MonoBehaviour
    {
        private EnemyHealth health;
        private NavMeshAgent agent;
        private Renderer[] renderers;
        private Color[] colors;
        private MaterialPropertyBlock block;
        private Transform body;
        private Quaternion deathPose;
        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            agent = GetComponent<NavMeshAgent>();
            body = transform.Find("Body");
            renderers = GetComponentsInChildren<Renderer>();
            colors = new Color[renderers.Length];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = renderers[i].sharedMaterial.color;
            block = new MaterialPropertyBlock();
            health.Died += SetDeathPose;
        }

        private void SetDeathPose() { deathPose = Quaternion.AngleAxis(85, transform.forward) * transform.rotation; }
        private void OnDestroy() { if (health) health.Died -= SetDeathPose; }

        private void Update()
        {
            if (!GameManager.Instance || !GameManager.Instance.IsPlaying)
                return;
            float flash = Mathf.Clamp01(1 - (Time.time - health.HurtAt) / .18f);
            for (int i = 0; i < renderers.Length; i++)
            {
                block.SetColor("_BaseColor", Color.Lerp(colors[i], new Color(1, .65f, .4f), flash * .65f));
                renderers[i].SetPropertyBlock(block);
            }

            if (!body)
                return;
            if (!health.Alive)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, deathPose, Time.deltaTime * 8);
                return;
            }

            body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 10) * Mathf.Min(1, agent.velocity.magnitude) * 3);
        }
    }
}
