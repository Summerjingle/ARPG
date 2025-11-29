using UnityEngine;

public class DialogueBubble : MonoBehaviour
{
    [Header("目标设置")]
    public Transform player;

    [Header("大小控制")]
    public float maxDistance = 10f;
    public float minDistance = 2f;
    public float maxScale = 1.5f;
    public float minScale = 1f;

    [Header("平滑过渡")]
    public float rotationSmoothness = 5f;
    public float scaleSmoothness = 5f;

    private Vector3 originalScale;
    private Vector3 fixedPosition;

    void Start()
    {
        originalScale = transform.localScale;
        fixedPosition = transform.position;
    }

    void Update()
    {
        if (player == null) return;

        // 冻结位置
        transform.position = fixedPosition;

        UpdateRotation();
        UpdateScale();
    }

    void UpdateRotation()
    {
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0;

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothness * Time.deltaTime);
        }
    }

    void UpdateScale()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        float scaleFactor = CalculateScaleFactor(distance);
        Vector3 targetScale = originalScale * scaleFactor;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSmoothness * Time.deltaTime);
    }

    float CalculateScaleFactor(float distance)
    {
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        float normalizedDistance = (distance - minDistance) / (maxDistance - minDistance);
        return Mathf.Lerp(minScale, maxScale, normalizedDistance);
    }
}