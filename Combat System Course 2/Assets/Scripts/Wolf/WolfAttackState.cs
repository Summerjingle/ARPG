using UnityEngine;
using System.Collections;

public class WolfAttackState : State<WolfController>
{
    private WolfController wolf;
    private bool isAttacking = false;

    public override void Enter(WolfController owner)
    {
        wolf = owner;
        isAttacking = true;
        wolf.Animator.SetTrigger("Attack");
        wolf.NavAgent.isStopped = true;

        // �������
        if (wolf.Player != null)
        {
            Vector3 direction = (wolf.Player.position - wolf.transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                wolf.transform.rotation = Quaternion.LookRotation(direction);
            }

            // �ؼ�������MeleeFighter�Ĺ���Ŀ��
            var playerFighter = wolf.Player.GetComponent<PlayerFighterNew>();
            if (playerFighter != null)
            {
                wolf.Fighter.currTarget = playerFighter;
            }
        }

        // ���ù�����ȴ
        wolf.AttackTimer = wolf.attackCooldown;

        // ����Э�̵ȴ��������
        wolf.StartCoroutine(WaitForAttackCompletion());
    }

    IEnumerator WaitForAttackCompletion()
    {
        // �ȴ�����������ɣ�������Ҫ�������ʵ�ʶ������ȵ�����
        yield return new WaitForSeconds(1f); // ���蹥��������Լ1��

        isAttacking = false;

        // ������ɺ����Ƿ�Ӧ�ü���׷��
        if (wolf.Player != null && !wolf.IsDead)
        {
            float distanceToPlayer = Vector3.Distance(wolf.transform.position, wolf.Player.position);

            if (distanceToPlayer > wolf.attackDistance && distanceToPlayer <= wolf.giveUpDistance)
            {
                // ����ڹ�����Χ�⵫���ڷ��������ڣ�����׷��
                wolf.ChangeState(WolfStates.Run);
            }
            else if (distanceToPlayer > wolf.giveUpDistance)
            {
                // ��ҳ����������룬�ص�Ѳ��ģʽ
                wolf.Mode = WolfMode.Patrol;
                wolf.ChangeState(WolfStates.Idle);

                // �� EnemyManager �Ƴ�
                if (wolf.EnemyController != null)
                {
                    EnemyManager.i.RemoveEnemyInRange(wolf.EnemyController);
                }
            }
            else
            {
                // ������ڹ�����Χ�ڣ������ٴι����򱣳־���
                wolf.ChangeState(WolfStates.Idle);
            }
        }
        else
        {
            wolf.ChangeState(WolfStates.Idle);
        }
    }

    public override void Execute()
    {
        // ���������в�ִ�������߼����ȴ�Э�����
        if (!isAttacking && !wolf.IsDead)
        {
            // ����������������ȷ��״̬��ȷ
            wolf.ChangeState(WolfStates.Idle);
        }
    }

    public override void Exit()
    {
        // ȷ��������ײ�屻����
        wolf.DisableAttackCollider();

        // ֹͣ����Э��
        wolf.StopAllCoroutines();
    }
}