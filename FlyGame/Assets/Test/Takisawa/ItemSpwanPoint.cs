using UnityEngine;
using UnityEngine.Serialization;

public class ItemSpwanPoint : MonoBehaviour
{
    [Header("基本設定")]
    [SerializeField] private GameObject itemPrefab;

    [FormerlySerializedAs("spawnInterval")]
    [SerializeField, Min(0f)] private float initialSpawnDelay = 5f;

    [Header("再生成条件")]
    [SerializeField] private Transform player;
    [SerializeField, Min(0.1f)] private float respawnDistance = 20f;

    [Header("配置先の地形")]
    [SerializeField] private MeshFilter surfaceMesh;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.1f;

    private GameObject currentItem;
    private Mesh cachedMesh;
    private Vector3[] vertices;
    private int[] triangles;
    private bool hasWarned;
    private bool initialized;
    private bool hasSpawned;
    private float firstSpawnTime;
    private Vector3 lastSpawnPosition;

    private void OnEnable()
    {
        initialized = false;

        if (itemPrefab == null || surfaceMesh == null || player == null)
        {
            Debug.LogError(
                "Item Prefab・Surface Mesh・Playerをすべて設定してください。",
                this
            );
            return;
        }

        firstSpawnTime = Time.time + Mathf.Max(0f, initialSpawnDelay);
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || currentItem != null)
            return;

        if (itemPrefab == null || surfaceMesh == null || player == null)
            return;

        if (!hasSpawned)
        {
            // 初回だけ指定した秒数を待つ。
            if (Time.time < firstSpawnTime)
                return;
        }
        else
        {
            // アイテム消滅後は、前回の出現位置からの直線距離で判定。
            float distance =
                Vector3.Distance(player.position, lastSpawnPosition);

            if (distance < Mathf.Max(0.1f, respawnDistance))
                return;
        }

        SpawnItem();
    }

    private void SpawnItem()
    {
        if (currentItem != null)
            return;

        if (!TryGetSurface(out Vector3 point, out Vector3 normal))
        {
            if (!hasWarned)
            {
                Debug.LogWarning(
                    "地形メッシュが未生成、読み取り不可、またはスポーン地点が範囲外です。",
                    this
                );
                hasWarned = true;
            }

            return;
        }

        hasWarned = false;

        Vector3 position = point + normal * surfaceOffset;

        Quaternion rotation =
            Quaternion.FromToRotation(Vector3.up, normal)
            * itemPrefab.transform.rotation;

        currentItem = Instantiate(itemPrefab, position, rotation);

        lastSpawnPosition = position;
        hasSpawned = true;
    }

    private bool TryGetSurface(out Vector3 point, out Vector3 normal)
    {
        point = Vector3.zero;
        normal = Vector3.up;

        if (surfaceMesh == null)
            return false;

        Mesh mesh = surfaceMesh.sharedMesh;

        if (mesh == null || !mesh.isReadable)
            return false;

        if (cachedMesh != mesh)
        {
            vertices = mesh.vertices;
            triangles = mesh.triangles;
            cachedMesh = mesh;
        }

        Transform surface = surfaceMesh.transform;
        Vector3 p = surface.InverseTransformPoint(transform.position);

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];

            float denominator =
                (b.z - c.z) * (a.x - c.x)
                + (c.x - b.x) * (a.z - c.z);

            if (Mathf.Abs(denominator) < 0.00000001f)
                continue;

            float wa =
                ((b.z - c.z) * (p.x - c.x)
                + (c.x - b.x) * (p.z - c.z))
                / denominator;

            float wb =
                ((c.z - a.z) * (p.x - c.x)
                + (a.x - c.x) * (p.z - c.z))
                / denominator;

            float wc = 1f - wa - wb;

            const float tolerance = 0.00001f;

            if (wa < -tolerance || wb < -tolerance || wc < -tolerance)
                continue;

            point = surface.TransformPoint(a * wa + b * wb + c * wc);

            Vector3 worldAB = surface.TransformVector(b - a);
            Vector3 worldAC = surface.TransformVector(c - a);

            normal = Vector3.Cross(worldAB, worldAC).normalized;

            return normal.sqrMagnitude > 0.0001f;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        bool found = TryGetSurface(
            out Vector3 point,
            out Vector3 normal
        );

        Gizmos.color = found ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.12f);

        if (!found)
            return;

        Vector3 position = point + normal * surfaceOffset;

        Gizmos.DrawLine(transform.position, position);
        Gizmos.DrawSphere(position, 0.08f);

        // 黄色い球の外側が、再生成を許可する距離の範囲。
        Gizmos.color = Color.yellow;

        Vector3 distanceOrigin = Application.isPlaying && hasSpawned
            ? lastSpawnPosition
            : position;

        Gizmos.DrawWireSphere(
            distanceOrigin,
            Mathf.Max(0.1f, respawnDistance)
        );
    }
}