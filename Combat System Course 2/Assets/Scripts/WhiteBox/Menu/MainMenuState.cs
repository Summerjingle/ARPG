using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuState : MonoBehaviour
{
    public enum State
    {
        PressAnyKey,
        Transition,
        MainMenu
    }

    public static bool skipPressAnyKey = false;

    public State currentState;

    public GameObject pressAnyKeyHint;
    public GameObject menuRoot;
    public ParticleSystem pressAnyKeyParticle; 
    public Transform particleSpawnPoint;       

    private PlayerInputActions input;
    private Animator hitAnimator;
    

    void Awake()
    {
        input = new PlayerInputActions();
        hitAnimator=pressAnyKeyHint.GetComponent<Animator>();
        
        

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
        input.UI.Enable();
        input.UI.AnyKey.performed += OnAnyKey;
    }

    void OnDisable()
    {
        input.UI.AnyKey.performed -= OnAnyKey;
        input.UI.Disable();
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
        SpawnParticleEffect();
        menuRoot.SetActive(true);
        
    }

    private void SpawnParticleEffect() 
    {
        ParticleSystem particleInstance = Instantiate(
            pressAnyKeyParticle,
            particleSpawnPoint.position,
            particleSpawnPoint.rotation);
        particleInstance.Play();
    }
}
