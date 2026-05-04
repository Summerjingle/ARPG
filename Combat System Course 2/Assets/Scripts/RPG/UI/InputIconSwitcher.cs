using UnityEngine;
using UnityEngine.UI;

public class InputIconSwitcher : MonoBehaviour
{
    [Header("UI 引用")]
    public Image targetImage;

    [Header("图标资源")]
    public Sprite keyboardIcon;
    public Sprite gamepadIcon;

    private void Start()
    {
        // 游戏开始时，获取当前的默认设备状态并初始化图标
        if (InputManager.Instance != null)
        {
            UpdateIcon(InputManager.Instance.IsUsingGamepad);
            
            // 订阅设备切换事件
            InputManager.Instance.OnDeviceChanged += UpdateIcon;
        }
    }

    private void OnDestroy()
    {
        // 销毁时取消订阅，防止内存泄漏
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnDeviceChanged -= UpdateIcon;
        }
    }

    // 事件触发时调用的方法
    private void UpdateIcon(bool isGamepad)
    {
        if (targetImage == null) return;

        targetImage.sprite = isGamepad ? gamepadIcon : keyboardIcon;
    }
}