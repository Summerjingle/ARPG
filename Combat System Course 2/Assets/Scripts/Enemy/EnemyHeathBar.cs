using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHeathBar : MonoBehaviour
{
    public TextMeshProUGUI myName;
    [Header("Ѫ������")]
    public Image healthBarFill;
    public Image healthBarBG;

    
    private HealthSystem healthSystem;

    [Header("λ��ƫ��")]
    public Vector3 positionOffset = new Vector3(0, 2f, 0);
    public bool faceCamera = true;

    [Header("�߼�����")]
    public bool hideWhenFull = true;
    public float showDurationAfterHit = 3f;
    private float hideTimer;
    private Canvas canvas;
    private Camera mainCamera;

    void Start()
    {
        // �Զ���ȡ���
        canvas = GetComponent<Canvas>();
        mainCamera = Camera.main;

        // ��ȡ HealthSystem
        healthSystem = GetComponentInParent<HealthSystem>();
        if (healthSystem == null )
        {
            Debug.Log("û�ҵ�health system");
        }

        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += OnHealthChanged; // ֱ�Ӷ���
            healthSystem.OnDeath += OnFighterDeath;
            healthSystem.OnDeathComplete += OnFighterDeathComplete;
            RefreshHealthBar();
        }


        // ����Canvas
        if (canvas != null)
        {
            canvas.worldCamera = mainCamera;
        }

        healthBarFill.enabled = false;
        healthBarBG.enabled = false;
    }

    void Update()
    {
        // �����ɫ�����������ٸ���λ�ú���ת
        if (healthSystem != null && healthSystem.IsDead) return;

        // ����Ѫ��λ�ã������ɫ��
        transform.position = healthSystem.transform.position + positionOffset;
        // ��Ѫ��ʼ���������
        if (faceCamera && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }

        if (myName != null)
        {
            myName.transform.rotation = mainCamera.transform.rotation;
        }

        if (hideWhenFull && canvas != null && healthSystem != null)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0 && healthSystem.Health >= healthSystem.MaxHealth)
            {
                healthBarFill.enabled = false;
                healthBarBG.enabled = false;
            }
        }
    }

    void OnHealthChanged(HealthSystem hs, HealthChangeInfo info)
    {
        RefreshHealthBar();
    }

   
    void OnFighterDeath(HealthSystem healthSystem)
    {
        // ��Ȼ���������ò�������Ϊ��ƥ���¼�ǩ���������
        if (canvas != null) canvas.enabled = false;
    }

  
    void OnFighterDeathComplete(HealthSystem healthSystem)
    {
        Debug.Log("Death sequence completed, health bar hidden");
    }

    void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= OnHealthChanged;
            healthSystem.OnDeath -= OnFighterDeath;
            healthSystem.OnDeathComplete -= OnFighterDeathComplete;
        }
    }

    // �ֶ�����Ѫ�����ⲿ���ã�
    public void RefreshHealthBar()
    {
        if (healthSystem != null && healthSystem.IsDead) return;
        if (healthBarFill == null || healthSystem == null) return;


        // ����Ѫ���ٷֱ�
        float fillAmount = healthSystem.Health / healthSystem.MaxHealth;
        healthBarFill.fillAmount = fillAmount;

        if (hideWhenFull && canvas != null)
        {
            healthBarFill.enabled = true;
            healthBarBG.enabled = true;
            hideTimer = showDurationAfterHit;
        }
    }
}