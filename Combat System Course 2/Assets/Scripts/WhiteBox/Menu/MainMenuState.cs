using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MainMenuState : MonoBehaviour
{
    public enum State
    {
        PressAnyKey,
        Transition,
        MainMenu
    }
    public Volume globalVolume;
    private DepthOfField depthOfFieldComponent; 

    public static bool skipPressAnyKey = false;

    public State currentState;

    public GameObject pressAnyKeyHint;
    public GameObject Title;
    public GameObject menuRoot;
    
      

    private PlayerInputActions input;
    private Animator hitAnimator;
    

    void Awake()
    {
        input = new PlayerInputActions();
        hitAnimator=pressAnyKeyHint.GetComponent<Animator>();
        if (globalVolume.profile.TryGet<DepthOfField>(out depthOfFieldComponent))
        {Debug.Log("成功拿到景深组件！");}
        Title.SetActive(true);
        

    }

    void Start()
    {
        if (skipPressAnyKey)
            EnterMainMenu();
        else
            EnterPressAnyKey();
    }

    void OnEnable()
    {
        input.UI_MainMenu.Enable();
        input.UI_MainMenu.AnyKey.performed += OnAnyKey;
    }

    void OnDisable()
    {
        input.UI_MainMenu.AnyKey.performed -= OnAnyKey;
        input.UI_MainMenu.Disable();
    }

    void OnAnyKey(InputAction.CallbackContext ctx)
    {
        if (currentState != State.PressAnyKey)
            return;

        PlayPressAnyKey();
    }

    void PlayPressAnyKey()
    {
        currentState = State.Transition;
        hitAnimator.SetTrigger("pressed");
        
    }
    void EnterPressAnyKey()
    {
        currentState = State.PressAnyKey;
        pressAnyKeyHint.SetActive(true);
        menuRoot.SetActive(false);
    }

    public void EnterMainMenu()
{
    currentState = State.MainMenu;
    pressAnyKeyHint.SetActive(false);
    Title.SetActive(false);
    menuRoot.SetActive(true);
    
    // 在 menuRoot 上启动协程（它刚刚被激活）
    menuRoot.GetComponent<MonoBehaviour>()?.StartCoroutine(SmoothFocusDistance(2f, 15f, 1.5f));
    // 或者更简单：让 menuRoot 挂一个专门的脚本，或者直接这样做：
}

IEnumerator SmoothFocusDistance(float from, float to, float duration)
{
    if (depthOfFieldComponent == null) yield break;
    
    float elapsed = 0;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        depthOfFieldComponent.focusDistance.value = Mathf.Lerp(from, to, t);
        yield return null;
    }
    depthOfFieldComponent.focusDistance.value = to;
}

    
}
