using UnityEngine;

/// <summary>
/// 挂在敌人身上：通过死亡动画末尾的 Animation Event 调用 SpawnSoulParticles() 生成光点，飞向玩家。
/// </summary>
public class SoulAbsorptionEffect : MonoBehaviour
{
    [Header("粒子预制体")]
    [SerializeField] private GameObject soulParticlePrefab;

    [Header("生成偏移")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1f, 0f); // 敌人胸口位置

    /// <summary>动画事件调用：在死亡动画末尾生成灵魂粒子</summary>
    public void SpawnSoulParticles()
    {
        if (soulParticlePrefab == null)
        {
            Debug.LogWarning($"[SoulAbsorption] {gameObject.name}: 未指定 soulParticlePrefab", this);
            return;
        }

        GameObject go = Instantiate(soulParticlePrefab, transform.position + spawnOffset, Quaternion.identity);

        SoulParticleAttractor attractor = go.GetComponent<SoulParticleAttractor>();
        if (attractor != null && PlayerController.i != null)
        {
            attractor.SetTarget(PlayerController.i.transform);
        }
    }
}
