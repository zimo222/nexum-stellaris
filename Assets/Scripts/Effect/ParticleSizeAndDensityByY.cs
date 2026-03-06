using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleSizeAndDensityByY : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;

    // 可调节的参数
    public float minHeight = 0f;       // Y 轴最低参考值
    public float maxHeight = 10f;      // Y 轴最高参考值
    public float minSize = 0.2f;       // 最小尺寸
    public float maxSize = 1.5f;       // 最大尺寸
    public float minAlpha = 0.2f;       // 最小透明度（密度）
    public float maxAlpha = 1f;         // 最大透明度（密度）
    public bool useWorldSpace = true;   // 是否使用世界坐标的 Y 轴

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        // 如果粒子数量可能变化，需要确保数组足够大
        if (particles == null || particles.Length < ps.main.maxParticles)
            particles = new ParticleSystem.Particle[ps.main.maxParticles];

        int numParticles = ps.GetParticles(particles);

        for (int i = 0; i < numParticles; i++)
        {
            // 获取粒子的位置
            Vector3 position = particles[i].position;
            if (useWorldSpace)
            {
                // 将本地坐标转换为世界坐标（如果模拟空间为本地）
                if (ps.main.simulationSpace == ParticleSystemSimulationSpace.Local)
                    position = transform.TransformPoint(position);
            }
            else
            {
                // 使用本地坐标
                if (ps.main.simulationSpace == ParticleSystemSimulationSpace.World)
                    position = transform.InverseTransformPoint(position);
            }

            float y = position.y;

            // 计算影响因子（将 Y 值钳位到 minHeight 和 maxHeight 之间，再映射到 0-1）
            float t = Mathf.InverseLerp(minHeight, maxHeight, y);

            // 根据因子调整大小
            float newSize = Mathf.Lerp(minSize, maxSize, t);
            particles[i].startSize = newSize;

            // 根据因子调整透明度（密度）
            Color currentColor = particles[i].startColor;
            currentColor.a = Mathf.Lerp(minAlpha, maxAlpha, t);
            particles[i].startColor = currentColor;
        }

        // 将修改后的粒子数据写回系统
        ps.SetParticles(particles, numParticles);
    }
}