using UnityEngine;

public class WeaponEquipmentManager : MonoBehaviour
{
    public static WeaponEquipmentManager Instance { get; private set; }

    [Header("配置点位")]
    public Transform weaponSocket; // 手部武器生成点
    public Transform weaponSocket2;// 手部武器生成点2
    public Transform weaponHolder;//腰部武器生成点
    public Transform weaponHolder_Back;//背部武器生成点
    private Weapon currentWeapon;
    public bool isWeaponDrawn = false;

    private Animator playerAnim;
    private bool isPlayingAnim;
    private float lastToggleTime = 0f;
    [SerializeField]private float toggleCooldown = 0.3f; // 动画结束后的防抖缓冲区，大剑动画长自然冷却就长

    [Header("重型武器")]
    [SerializeField] private float heavyWeaponSpeedMultiplier = 0.7f; // 拔出重型武器时移速倍率
    public float CurrentSpeedMultiplier =>
        (currentWeapon != null && currentWeapon.isHeavy && (isWeaponDrawn || isPlayingAnim)) ? heavyWeaponSpeedMultiplier : 1f;

    // ===== 根据武器配置选挂点 =====
    // 新增收纳挂点：加 case SheathLocation.Xxx => weaponHolder_Xxx;
    Transform GetSheathHolder(Weapon w) => w.sheathLocation switch
    {
        SheathLocation.Back => weaponHolder_Back,
        _ => weaponHolder,
    };

    // 新增手部socket：加 case HandSocket.Xxx => weaponSocketXxx;
    Transform GetHandSocket(Weapon w) => w.handSocket switch
    {
        HandSocket.Secondary => weaponSocket2,
        _ => weaponSocket,
    };



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
    private void OnEnable()
    {
        InputManager.Instance.ToggleWeapon += ToggleWeapon;
    }

    private void OnDisable()
    {
        InputManager.Instance.ToggleWeapon -= ToggleWeapon;
    }
    void Start()
    {
        playerAnim=GetComponent<Animator>();
    }


    public void EquipWeapon(ItemSO weaponItem)
    {
        WeaponSO weaponSO = weaponItem as WeaponSO;
        if (weaponSO == null) return;

        UnequipWeapon();

        if (weaponSO.equipmentPrefab == null)
        {
            Debug.LogError("武器物品的预制体为空");
            return;
        }

        // 先统一从腰间实例化，下面再根据武器配置移到正确的挂点
        GameObject weaponObj = Instantiate(
            weaponSO.equipmentPrefab,
            weaponHolder.position,
            weaponHolder.rotation,
            weaponHolder
        );

        currentWeapon = weaponObj.GetComponent<Weapon>();
        if (currentWeapon != null)
        {
            currentWeapon.Initialize(weaponSO);

            // 根据武器配置切换到正确的收纳挂点（如背部大剑）
            Transform holder = GetSheathHolder(currentWeapon);
            if (holder != weaponHolder)
            {
                weaponObj.transform.SetParent(holder);
                weaponObj.transform.localPosition = Vector3.zero;
                weaponObj.transform.localRotation = Quaternion.identity;
            }
        }

        //UI更新暂时先留着，尽管武器并没有实例化到手上（真正拿起）
        InventoryUI.Instance?.UpdateEquipmentIcon(weaponSO);
        BackpackCharacterDisplay.Instance?.EquipWeapon(weaponSO);
    }

    // 卸下武器
    public void UnequipWeapon()
    {
        if (currentWeapon != null)
        {

            // 如果武器在手上，先触发放回的逻辑
            if (isWeaponDrawn)
            {
                playerAnim.SetTrigger(currentWeapon.sheathWeaponTriggerName);
                Debug.Log("收回武器");
            }
            // 将装备放回背包
            if (InventoryManager.Instance != null && currentWeapon.itemSO != null)
            {
                InventoryManager.Instance.ReAddItem(currentWeapon.itemSO);
            }

            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
        }
        isWeaponDrawn = false;
        isPlayingAnim = false;

        // UI更新
        InventoryUI.Instance?.ClearEquipmentIcon(ItemType.Weapon, ArmorType.NotArmor);
        BackpackCharacterDisplay.Instance?.UnequipWeapon();
    }
    public void ToggleWeapon()
    {
        if (Time.time < lastToggleTime + toggleCooldown) return;
        if (currentWeapon == null)
        {
            Debug.Log("没有装备武器");
            return;
        }

        if (isPlayingAnim) return;

        isPlayingAnim = true;

        if (isWeaponDrawn)
            playerAnim.SetTrigger(currentWeapon.sheathWeaponTriggerName);

        else
            playerAnim.SetTrigger(currentWeapon.drawWeaponTriggerName);
    }
    public void DrawWeapon() // 动画事件：拔武器动画holder与socket交错的第一帧
    {
        if (currentWeapon != null && !isWeaponDrawn)
        {
            // 根据武器配置选正确的手部socket
            currentWeapon.transform.SetParent(GetHandSocket(currentWeapon));

            currentWeapon.transform.localPosition = Vector3.zero;
            currentWeapon.transform.localRotation = Quaternion.identity;

            Debug.Log("武器已拔出至手上");
        }
    }
    public void SheathWeapon() // 动画事件：收武器动画holder与socket交错的第一帧
    {
        if (currentWeapon != null && isWeaponDrawn)
        {
            // 根据武器配置选正确的收纳挂点（腰间/背部）
            currentWeapon.transform.SetParent(GetSheathHolder(currentWeapon));

            currentWeapon.transform.localPosition = Vector3.zero;
            currentWeapon.transform.localRotation = Quaternion.identity;

            Debug.Log("武器已收回");
        }
    }

    public void SetWeaponDrawState()// 动画事件：拔武器动画末帧
    {
        isWeaponDrawn = true;
        isPlayingAnim = false;
        lastToggleTime = Time.time;
        playerAnim.SetBool("Armed", true);
        Debug.Log("拔剑状态完成");
    }
    public void SetWeaponSheathState()// 动画事件：收武器动画末帧
    {
        isWeaponDrawn = false;
        isPlayingAnim = false;
        lastToggleTime = Time.time;
        playerAnim.SetBool("Armed", false);
        Debug.Log("收剑状态完成");
    }


    // 获取当前武器
    public Weapon GetCurrentWeapon() => currentWeapon;

    // 获取武器伤害
    public float GetWeaponDamage()
    {
        return currentWeapon?.GetDamage() ?? 5f;
    }
}
