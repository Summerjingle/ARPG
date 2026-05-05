using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Scrollbar))]
public class FIX_Scrollbar : MonoBehaviour
{
    [Tooltip("固定的滑块大小 (0-1)")]
    public float fixedSize = 0f;
    
    private Scrollbar scrollbar;
    
    void Awake()
    {
        scrollbar = GetComponent<Scrollbar>();
    }
    
    void LateUpdate()
    {
        // 每帧强制设置，对抗 ScrollRect 的覆盖
        if (scrollbar.size != fixedSize)
        {
            scrollbar.size = fixedSize;
        }
    }
    
    // 保留你的方法，方便手动调用
    public void setFixedHandleSize()
    {
        scrollbar.size = fixedSize;
    }
}