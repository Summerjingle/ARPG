using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public ItemSO itemSO; // Ìí¼ÓItemSOÒýÓÃ
    public abstract float GetDamage();
    public virtual void Initialize(ItemSO weaponItem)
    {
        itemSO = weaponItem;
    }
}