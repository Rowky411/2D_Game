using System.Collections.Generic;
using UnityEngine;

// Keeps up to maxAlive mobs inside a rectangular area, respawning them after they die.
public class MobSpawner : MonoBehaviour
{
    [SerializeField] private Mob[] mobPrefabs;
    [SerializeField] private Vector2 areaSize = new Vector2(8f, 6f);
    [SerializeField] private int maxAlive = 4;
    [SerializeField] private float respawnDelay = 10f;
    [Tooltip("Spawn points must be this far from any collider (walls, trees, other mobs).")]
    [SerializeField] private float spawnClearance = 0.45f;

    private readonly List<Mob> alive = new List<Mob>();
    private float nextSpawnTime;

    private void Start()
    {
        for (int i = 0; i < maxAlive; i++) TrySpawn();
    }

    private void Update()
    {
        if (alive.Count >= maxAlive || Time.time < nextSpawnTime) return;
        TrySpawn();
        nextSpawnTime = Time.time + 1f;
    }

    private void TrySpawn()
    {
        if (mobPrefabs == null || mobPrefabs.Length == 0) return;

        for (int attempt = 0; attempt < 15; attempt++)
        {
            Vector2 point = (Vector2)transform.position + new Vector2(
                Random.Range(-0.5f, 0.5f) * areaSize.x,
                Random.Range(-0.5f, 0.5f) * areaSize.y);
            if (Physics2D.OverlapCircle(point, spawnClearance) != null) continue;

            var prefab = mobPrefabs[Random.Range(0, mobPrefabs.Length)];
            var mob = Instantiate(prefab, point, Quaternion.identity, transform);
            mob.Died += OnMobDied;
            alive.Add(mob);
            return;
        }
    }

    private void OnMobDied(Mob mob)
    {
        alive.Remove(mob);
        nextSpawnTime = Time.time + respawnDelay;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}
