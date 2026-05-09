using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkourController : MonoBehaviour
{
    [SerializeField] List<ParkourAction> parkourActions;
    private EnvironmentScanner environmentScanner;
    private Animator animator;
    private ICombatSystem combatSystem;
    private PlayerController playerController;
    private void Awake()
    {
        environmentScanner = GetComponent<EnvironmentScanner>();
        animator = GetComponent<Animator>();
        combatSystem=GetComponent<ICombatSystem>();
        playerController = GetComponent<PlayerController>();
    }
    
    IEnumerator DoParkourAction(ParkourAction action)
    {
        combatSystem.InAction = true;
        UIStateManager.SetUIActive(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        animator.CrossFade(action.AnimName, 0.2f);

        // �ȴ��������
        yield return new WaitUntil(() => !animator.IsInTransition(0));

        var animState = animator.GetCurrentAnimatorStateInfo(0);
        float timer = 0f;
        bool hasMatched = false;

        while (timer <= animState.length)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / animState.length;

            if (action.RotateToObstacle)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, action.TargetRotation, playerController.  RotationSpeed * Time.deltaTime);

            if (action.EnableTargetMatching && !hasMatched)
            {
                // ֻ���ض�ʱ�䴰����ƥ�䣬��ȷ�����ڹ�����
                if (normalizedTime >= action.MacthStartTime && normalizedTime <= action.MacthTargetTime)
                {
                    if (!animator.IsInTransition(0) && !animator.isMatchingTarget)
                    {
                        MatchTarget(action);
                    }
                }
                else if (normalizedTime > action.MacthTargetTime)
                {
                    hasMatched = true;
                }
            }

            yield return null;
        }

        yield return new WaitForSeconds(action.PostActionDelay);
        combatSystem.InAction = false;
        UIStateManager.SetUIActive(false);
    }
    public bool TryClimb()
    {
        if (combatSystem.InAction)
            return false;

        var hitData = environmentScanner.ObstacleCheck();
        if (!hitData.forwardHitFound)
            return false;

        foreach (var action in parkourActions)
        {
            if (action.CheckIfPossible(hitData, transform))
            {
                StartCoroutine(DoParkourAction(action));
                return true; // �ɹ�����
            }
        }

        return false; // û�ҵ����� �� ������
    }
    private void MatchTarget(ParkourAction action)
    {
        if (animator.isMatchingTarget) return;
        animator.MatchTarget(action.MatchPos, transform.rotation, action.MatchBodyPart, new MatchTargetWeightMask((action.MatchPosWight),0),action.MacthStartTime,action.MacthTargetTime);
    }
}
