using UnityEngine;

namespace TacticalZero
{
    public sealed class PrototypeCatalog : ScriptableObject
    {
        [SerializeField]
        private WeaponDefinition[] weapons;
        [SerializeField]
        private GameObject playerPrefab;
        [SerializeField]
        private GameObject[] enemyPrefabs;
        [SerializeField]
        private GameObject grassPrefab;
        [SerializeField]
        private GameObject[] pickupPrefabs;
        public WeaponDefinition[] Weapons => weapons;
        public GameObject PlayerPrefab => playerPrefab;
        public GameObject[] EnemyPrefabs => enemyPrefabs;
        public GameObject GrassPrefab => grassPrefab;
        public GameObject[] PickupPrefabs => pickupPrefabs;

        public void FillMissing(WeaponDefinition[] guns, GameObject player, GameObject[] enemies, GameObject grass, GameObject[] pickups)
        {
            if (weapons == null || weapons.Length == 0)
                weapons = guns;
            else
                for (int i = 0; i < weapons.Length && i < guns.Length; i++)
                    if (!weapons[i])
                        weapons[i] = guns[i];
            if (!playerPrefab)
                playerPrefab = player;
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
                enemyPrefabs = enemies;
            else
                for (int i = 0; i < enemyPrefabs.Length && i < enemies.Length; i++)
                    if (!enemyPrefabs[i])
                        enemyPrefabs[i] = enemies[i];
            if (!grassPrefab)
                grassPrefab = grass;
            if (pickupPrefabs == null || pickupPrefabs.Length == 0)
                pickupPrefabs = pickups;
            else
                for (int i = 0; i < pickupPrefabs.Length && i < pickups.Length; i++)
                    if (!pickupPrefabs[i])
                        pickupPrefabs[i] = pickups[i];
        }
    }
}
