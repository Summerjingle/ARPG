using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonGroupManager : MonoBehaviour
{
    [Header("按钮配置")]
    public Animator[] buttons; // 按顺序：[按钮1, 按钮2, 按钮3]

    private int currentSelected = 0; // 默认选中第一个按钮

    void Start()
    {
        // 初始化：选中第一个按钮，其他取消选中
        SelectButton(0);
    }

    // 核心方法：选中指定按钮
    public void SelectButton(int index)
    {
        // 取消之前选中的按钮
        buttons[currentSelected].SetBool("Selected", false);

        // 选中新按钮
        currentSelected = index;
        buttons[currentSelected].SetBool("Selected", true);
    }

    // 触发按钮按压动画
    public void PressButton(int index)
    {
        buttons[index].SetTrigger("Press");
    }

    // 自动挂载到按钮的事件系统（无需额外脚本！）
    public void OnButtonHover(int index) => SelectButton(index);
    public void OnButtonClick(int index) => PressButton(index);
}