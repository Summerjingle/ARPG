using UnityEngine;

public class WeaponEquipmentManager : MonoBehaviour
{
    public static WeaponEquipmentManager Instance { get; private set; }

    [Header("配置点位")]
    public Transform weaponSocket; // 手部武器生成点
    public Transform weaponHolder;//腰部武器生成点
    private Weapon currentWeapon;
    public bool isWeaponDrawn = false;
    
    private Animator playerAnim;
    private bool isPlayingAnim; 
    private float lastToggleTime = 0f;
    private float toggleCooldown = 3.3f; // 3.2秒内不能重复切换
    
    
    private GameObject currentWeaponInHand;
    private GameObject currentWeaponInSheath;

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
        UnequipWeapon();

        if (weaponItem?.weaponPrefab == null)
        {
            Debug.LogError("������Ʒ��Ԥ����Ϊ��");
            return;
        }

        
        GameObject weaponObj = Instantiate(
            weaponItem.weaponPrefab,
            // weaponSocket.position,
            // weaponSocket.rotation,
            // weaponSocket
            weaponHolder.position,
            weaponHolder.rotation,
            weaponHolder
        
        );

        currentWeapon = weaponObj.GetComponent<Weapon>();
        if (currentWeapon != null)
        {
            currentWeapon.Initialize(weaponItem);
        }

        //UI更新暂时先留着，尽管武器并没有实例化到手上（真正拿起）
        InventoryUI.Instance?.UpdateEquipmentIcon(weaponItem);
        //PlayerController.i.SetArmedMode(true);
        //现在不通过这里改变角色动画状态。这里仅用作将武器实例化至腰部
    }

    // ж������
    public void UnequipWeapon()
    {
        if (currentWeapon != null)
        {

            // 如果武器在手上，先触发放回腰间的逻辑
            if (isWeaponDrawn)
            {                                                                                             
                playerAnim.SetTrigger("sheathWeapon");
                Debug.Log("收回武器");
            }                                         
            // ���������ӻر���
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
        InventoryUI.Instance.currentSelectedItem=null;
        InventoryUI.Instance.SelectFirstItem();
        //PlayerController.i.SetArmedMode(false);
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
            playerAnim.SetTrigger("sheathWeapon");
            
        else
            playerAnim.SetTrigger("drawWeapon");
    }
    public void DrawWeapon() //Draw2第一帧
    {
        if (currentWeapon != null && !isWeaponDrawn)
        {
            // 将武器父物体切换到手部挂点
            currentWeapon.transform.SetParent(weaponSocket);
            
            // 重置坐标和旋转，对齐手部
            currentWeapon.transform.localPosition = Vector3.zero;
            currentWeapon.transform.localRotation = Quaternion.identity;

            
            Debug.Log("武器已拔出至手上");
        }
    }
    public void SheathWeapon() // Sheath2第一帧
    {
        if (currentWeapon != null && isWeaponDrawn)
        {
            // 将武器父物体切换回腰部挂点
            currentWeapon.transform.SetParent(weaponHolder);

            // 重置坐标和旋转，对齐腰部
            currentWeapon.transform.localPosition = Vector3.zero;
            currentWeapon.transform.localRotation = Quaternion.identity;

            
            Debug.Log("武器已收到腰间");
        }
    }

    public void SetWeaponDrawState()//Draw2最后一帧
    {
        isWeaponDrawn = true;
        isPlayingAnim = false;
        playerAnim.SetBool("Armed", true);
    }
    public void SetWeaponSheathState()// Sheath2最后一帧
    {
        isWeaponDrawn = false;
        isPlayingAnim = false;
        playerAnim.SetBool("Armed", false);
    }


    // ��ȡ��ǰ����
    public Weapon GetCurrentWeapon() => currentWeapon;

    // ��ȡ�����˺�
    public float GetWeaponDamage()
    {
        return currentWeapon?.GetDamage() ?? 5f;
    }
}