using UnityEngine;

/// <summary>
/// 放在粒子预制体上：每帧将粒子吸引到目标位置，抵达后粒子消失。
/// </summary>
public class SoulParticleAttractor : MonoBehaviour
{
    [Header("时间")]
    [SerializeField] private float startDelay = 0.5f;       // 延迟多久后开始吸引（让粒子先散开）

    [Header("吸引参数")]
    [SerializeField] private float attractForce = 5f;       // 吸引速度
    [SerializeField] private float arrivalRadius = 0.5f;    // 到达判定距离
    [SerializeField] private float steerSpeed = 5f;         // 转向灵敏度
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.2f, 0f); // 目标点偏移（胸口）

    private Transform target;
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles = new ParticleSystem.Particle[64];
    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
        ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None; // 脚本控制销毁时机
        }
    }

    public void SetTarget(Transform t) => target = t;

    void Update()
    {
        if (ps == null) return;

        int count = ps.particleCount;
        if (count == 0)
        {
            Destroy(gameObject);
            return;
        }

        // 确保数组容量足够
        if (particles.Length < count)
            particles = new ParticleSystem.Particle[count * 2];

        ps.GetParticles(particles, count);

        Vector3 targetPos = target != null ? target.position + targetOffset : transform.position;

        // 延迟期间不吸引，让粒子先自由散开
        bool canAttract = Time.time >= spawnTime + startDelay;

        for (int i = 0; i < count; i++)
        {
            if (!canAttract) continue; // 延迟中，粒子自由运动

            Vector3 toTarget = targetPos - particles[i].position;
            float dist = toTarget.magnitude;

            if (dist < arrivalRadius)
            {
                // 抵达目标 → 消灭粒子
                particles[i].remainingLifetime = 0f;
            }
            else if (target != null)
            {
                // 向目标方向加速
                Vector3 desired = toTarget.normalized * attractForce;
                particles[i].velocity = Vector3.Lerp(particles[i].velocity, desired, steerSpeed * Time.deltaTime);

                // 临近目标时额外加速（避免粒子飘太久）
                float t = 1f - Mathf.Clamp01(dist / 3f);
                particles[i].velocity += desired * (t * Time.deltaTime * 3f);
            }
        }

        ps.SetParticles(particles, count);
    }
}
