using UnityEngine;

public class SimpleDissolve : MonoBehaviour
{
    public Material[] myMaterials;
    [Header("溶解速度")]
    public float dissolveSpeed = 0.5f;

    private bool _isDissolving = false;
    private float _currentProgress = 0f; // 私有变量，代替原来的 Range 滑块

    void Start()
    {
        // 初始确保是完全显示的 (0)
        SetMaterialsProgress(0f);
    }

    void Update()
    {
        if (_isDissolving)
        {
            // 随着时间增加数值，直到 1 (完全溶解)
            _currentProgress += Time.deltaTime * dissolveSpeed;
            
            SetMaterialsProgress(_currentProgress);

            // 彻底溶解后销毁
            if (_currentProgress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_isDissolving)
        {
            _isDissolving = true;
        }
    }

    // 抽离出来的设置材质函数
    void SetMaterialsProgress(float val)
    {
        if (myMaterials == null) return;
        
        foreach (Material mat in myMaterials)
        {
            if (mat != null)
            {
                mat.SetFloat("_BossPlaceDissolve", val);
            }
        }
    }
}