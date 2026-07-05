using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 血液特效静态工具 —— 在命中点生成 Splash 粒子 + 地面 DecalProjector 贴花。
/// 由 PlayerFighter / EnemyFighter 的 OnTriggerEnter 调用。
/// </summary>
public static class BloodEffectManager
{
    private const uint DecalLayerMask = (1 << 0) | (1 << 3); // Default + Ground

    public static void SpawnBlood(Vector3 hitPoint,
                                  GameObject[] splashPrefabs,
                                  GameObject[] decalPrefabs,
                                  float heightOffset = 0.07f,
                                  float decalLifetime = 30f)
    {
        // Splash
        if (splashPrefabs != null && splashPrefabs.Length > 0)
        {
            var p = splashPrefabs[Random.Range(0, splashPrefabs.Length)];
            if (p != null) Object.Instantiate(p, hitPoint, Quaternion.identity);
        }

        // DecalProjector
        if (decalPrefabs == null || decalPrefabs.Length == 0) return;

        Vector3 groundPoint = hitPoint;
        if (Physics.Raycast(hitPoint + Vector3.up * 0.3f, Vector3.down, out RaycastHit hit, 5f, -1,
                QueryTriggerInteraction.Ignore))
        {
            groundPoint = hit.point;
        }
        groundPoint.y += heightOffset;

        var prefab = decalPrefabs[Random.Range(0, decalPrefabs.Length)];
        if (prefab == null) return;

        Quaternion rot = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        var decal = Object.Instantiate(prefab, groundPoint, rot);

        // LayerMask
        var projector = decal.GetComponent<DecalProjector>();
        if (projector == null)
            projector = decal.GetComponentInChildren<DecalProjector>();
        if (projector != null)
        {
            projector.renderingLayerMask = DecalLayerMask;
        }

        // 应用 BloodModifier 的颜色参数
        var modifier = decal.GetComponent<BloodEffectsPack.BloodModifier_URP>();
        if (modifier != null) modifier.Apply();

        if (decalLifetime > 0f)
            Object.Destroy(decal, decalLifetime);
    }
}
