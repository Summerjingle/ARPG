using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WolfFighter : EnemyFighter
{
    private WolfController wolfController;

    protected override void Awake()
    {
        wolfController = GetComponent<WolfController>();
        base.Awake(); // ���ø����ʼ���������
    }

    // ֻ��Ҫ��д��ײ����ʼ�� - �ǲ���Ҫ������ײ��
    protected override void InitializeEnemyBodyColliders()
    {
        // ������������ײ����ʼ��
        Debug.Log($"�� {gameObject.name} ����������ײ����ʼ��");

        // ֻ��ʼ��������ײ��������еĻ���
        if (enemyWeapon != null)
        {
            WeaponCollider = enemyWeapon.GetComponentInChildren<BoxCollider>();
            if (WeaponCollider != null)
            {
                WeaponCollider.enabled = false;
                Debug.Log($"��ʼ����������ײ��: {WeaponCollider.name}");
            }
        }
    }
    public override void TakeDamage(float damage, bool isCrit = false)
    {
        if (HealthSystem.IsDead) return;

        base.TakeDamage(damage, isCrit);

        // ���������֪ͨ WolfController
        if (HealthSystem.IsDead && wolfController != null)
        {
            wolfController.HandleWolfDeath();
        }
    }
}
