using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFighter : MonoBehaviour, ICombatSystem
{
    private WeaponEquipmentManager weaponManager;
    private PlayerProperty playerProperty;

    private bool attackInput;
    private float lastAttackTime;
    private const float INPUT_BUFFER_TIME = 0.2f;
    private ICombatSystem currentTarget;

    private HashSet<int> hitTargets = new HashSet<int>();

     // ����ϵͳ
    public HealthSystem HealthSystem { get; private set; }
    
    // ս��״̬
    public bool InAction { get; set; } = false;
    public bool IsTakingHit { get; private set; } = false;
    public bool InCounter { get; set; } = false;
    public bool IsCounterable => Attackstate == AttackStates.Windup && comboCount == 0;
    public float CritRate => playerProperty?.TotalCritRate ?? 0f;
    public string CurrentSpecialHitReaction { get; set; }
    
    // ����״̬
    public AttackStates Attackstate { get; set; }
    public bool docombo { get; set; }
    public int comboCount { get; set; } = 0;

    // Ŀ�����
    public ICombatSystem currTarget { get; set; }

    // ��������
    [SerializeField] private List<AttackData> attacks;
    [SerializeField] private List<AttackData> longRangeAttacks;
    [SerializeField] private float longRangeAttackThreshold = 1.5f;
    
    public List<AttackData> Attacks => attacks;
    public List<AttackData> LongRangeAttacks => longRangeAttacks;
    public float LongRangeAttackThreshold => longRangeAttackThreshold;
    
    // �������
    public Animator animator { get; private set; }
    public BoxCollider WeaponCollider { get; private set; }
    public SphereCollider leftHandCollider { get; private set; }
    public SphereCollider rightHandCollider { get; private set; }
    public SphereCollider leftFootCollider { get; private set; }
    public SphereCollider rightFootCollider { get; private set; }
    
    // �¼�
    public event System.Action<ICombatSystem> OnGotHit;
    public event System.Action OnHitComplete;
    public event System.Action<GameObject> OnDamageDealt;
    public void NotifyDamageDealt(GameObject target) => OnDamageDealt?.Invoke(target);
    private void Awake()
    {
        weaponManager = WeaponEquipmentManager.Instance;
        playerProperty = GetComponent<PlayerProperty>();
        HealthSystem = GetComponent<HealthSystem>();
        if (HealthSystem == null)
            HealthSystem = gameObject.AddComponent<HealthSystem>();

        animator = GetComponent<Animator>();

        
        InitializeBodyColliders();
    }
    private void InitializeBodyColliders()
    {
        // �� MeleeFighter Ǩ�ƹ�������ײ����ʼ���߼�
        leftHandCollider = animator.GetBoneTransform(HumanBodyBones.LeftHand)?.GetComponent<SphereCollider>();
        leftFootCollider = animator.GetBoneTransform(HumanBodyBones.LeftFoot)?.GetComponent<SphereCollider>();
        rightHandCollider = animator.GetBoneTransform(HumanBodyBones.RightHand)?.GetComponent<SphereCollider>();
        rightFootCollider = animator.GetBoneTransform(HumanBodyBones.RightFoot)?.GetComponent<SphereCollider>();

        // ���ʹ��װ��������������
        var currentWeapon = WeaponEquipmentManager.Instance?.GetCurrentWeapon();
        if (currentWeapon != null)
        {
            WeaponCollider = currentWeapon.GetComponentInChildren<BoxCollider>();
        }
    }// ��ʼ����ײ��
    private void Update()
    {
        HandlePlayerInput();
        UpdateTarget();
    }

    private void HandlePlayerInput()
    {
        // ��⹥������
        if (Input.GetMouseButtonDown(1) && !IsUIActive())
        {
            attackInput = true;
            lastAttackTime = Time.time;
            Debug.Log("��ҹ��������⵽");
        }

        // ������������
        if (attackInput && Time.time - lastAttackTime <= INPUT_BUFFER_TIME)
        {
            if (PlayerCanAttack())
            {
               
                TryToAttack(currentTarget);
                attackInput = false;
            }
        }
        else if (attackInput)
        {
            // ���볬ʱ
            attackInput = false;
        }
    }

    private void UpdateTarget()
    {
        // �Զ�Ѱ�������Ŀ��
        currentTarget = FindNearestTarget();
    }

    // ������UI״̬���
    private bool IsUIActive()
    {
        return UIStateManager.IsAnyUIActive;
    }
    public void TakeDamage(float damage, bool isCrit = false)
    {
        if (HealthSystem.IsDead) return;

        int currentArmor = GetPlayerArmor();
        HealthSystem.TakeDamage(damage, currentArmor, isCrit);
        OnGotHit?.Invoke(this);  // this ���� ICombatSystem

       

        Debug.Log($"����ܵ��˺�: {damage}, ���׼���: {currentArmor}, ʣ������: {HealthSystem.Health}");
    }
    private ICombatSystem FindNearestTarget()
    {
        var enemies = FindObjectsOfType<EnemyFighter>();
        ICombatSystem nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            var fighter = enemy.GetComponent<ICombatSystem>();
            if (fighter != null && !fighter.HealthSystem.IsDead)
            {
                float distance = Vector3.Distance(transform.position, fighter.transform.position);
                if (distance < nearestDistance && distance < 8f) // 8����
                {
                    nearestDistance = distance;
                    nearest = fighter;
                }
            }
        }

        return nearest;
    }
    
    public int GetPlayerArmor()
    {
        return playerProperty?.armorValue ?? 0;
    }
    public ICombatSystem GetCurrentTarget()
    {
        return currentTarget;
    }

    public IEnumerator PlayHitReaction(ICombatSystem attacker, string specialHitReaction = null)
    {
        PlayerController.i?.OnRollEnd();

        InAction = true;
        IsTakingHit = true;

        var dispVec = attacker.transform.position - transform.position;
        dispVec.y = 0f;
        transform.rotation = Quaternion.LookRotation(dispVec);

        string hitAnim = string.IsNullOrEmpty(specialHitReaction) ? "SwordImpact" : specialHitReaction;
        animator.CrossFade(hitAnim, 0.2f, 1);
        yield return null;
        var animstate = animator.GetNextAnimatorStateInfo(1);
        yield return new WaitForSeconds(animstate.length * 0.8f);

        OnHitComplete?.Invoke();
        InAction = false;
        IsTakingHit = false;
    }
    public void PlayDeathAnimation(ICombatSystem attacker)
    {
        PlayerController.i?.OnRollEnd();
        animator.CrossFade("Death", 0.2f);
    }//������������
    private void OnTriggerEnter(Collider other)
    {
        if (HealthSystem.IsDead) return;
        
        if (other.tag == "Hitbox" && !IsTakingHit && !InCounter)
        {
            var attacker = other.GetComponentInParent<ICombatSystem>();
            if (attacker == null || attacker.currTarget == null) return;
            if (attacker.currTarget.gameObject != this.gameObject) return;

            var attackerDamage = attacker.GetWeaponDamage();

            // 防止同一刀命中同一目标多次
            if (!attacker.RegisterHit(this.gameObject)) return;

            bool isCrit = Random.value < (attacker.CritRate / 100f);
            TakeDamage(attackerDamage, isCrit);

            // 通知攻击方：成功造成伤害
            attacker.NotifyDamageDealt(this.gameObject);

            Debug.Log("�������");
            if (!HealthSystem.IsDead)
            {
                string specialReaction = attacker.CurrentSpecialHitReaction;
                attacker.CurrentSpecialHitReaction = null;
                StartCoroutine(PlayHitReaction(attacker, specialReaction));
            }
            else
            {
                PlayDeathAnimation(attacker);
            }
        }
    }
    public bool RegisterHit(GameObject target)
    {
        int id = target.GetInstanceID();
        if (hitTargets.Contains(id)) return false;
        hitTargets.Add(id);
        return true;
    }

    public float GetWeaponDamage()
    {
        return WeaponEquipmentManager.Instance?.GetWeaponDamage() ?? 1f;
    }

    public bool PlayerHasUsableWeapon()
    {
        return WeaponEquipmentManager.Instance?.GetCurrentWeapon() != null;
    }


    public bool PlayerCanAttack()
    {
        return !InAction && PlayerHasUsableWeapon();
    }


    public void PlayerTryToAttack(ICombatSystem target = null)
    {
        

        if (PlayerCanAttack())
        {
            StartCoroutine(ExecutePlayerAttack(target, comboCount));
        }
        else if (Attackstate == AttackStates.Impact ||
                 Attackstate == AttackStates.Cooldown)
        {
            docombo = true;
        }
    }


    public Vector3 CalculatePlayerAttackPosition(ICombatSystem target, AttackData attack, Vector3 attackDir, Vector3 startPos)
    {
        // �����Ҫ�ֶ������ƶ�λ��
        Vector3 targetPos = Vector3.zero;

        if (target != null && attack.MoveToTarget)
        {
            float distance = Vector3.Distance(target.transform.position, startPos);
            if (distance < attack.MaxMoveDistance)
                targetPos = target.transform.position - attackDir * attack.DistanceFromTarget;
            else
                targetPos = startPos + attackDir * attack.MaxMoveDistance;
        }

        return targetPos;
    }


    public Vector3 CalculatePlayerAttackDirection(ICombatSystem target)
    {
        if (target != null)
        {
            var vecToTarget = target.transform.position - transform.position;
            vecToTarget.y = 0;
            return vecToTarget.normalized;
        }
        return transform.forward;
    }

    // ��ҹ�������ѡ�� - ���Ǿ��������
    public AttackData SelectPlayerAttack(ICombatSystem target, List<AttackData> attacks, List<AttackData> longRangeAttacks, int comboCount, float longRangeThreshold)
    {
        
        var attack = attacks[comboCount % attacks.Count];

        // �����Ҫ���ݾ���ѡ���ս��Զ�̹���
        if (target != null)
        {
            float distance = Vector3.Distance(target.transform.position, transform.position);

            // ������볬����ֵ����Զ�̹������ã�ѡ��Զ�̹���
            if (distance > longRangeThreshold && longRangeAttacks.Count > 0)
            {
                attack = longRangeAttacks[0];
                Debug.Log($"���ѡ��Զ�̹���: {attack.AttackName}, ����: {distance}");
            }
            else
            {
                Debug.Log($"���ѡ���ս����: {attack.AttackName}, ������: {comboCount}");
            }
        }

        return attack;
    }

    // ��ҹ���׼���߼�
    public void PreparePlayerAttack(ICombatSystem target)
    {
        

        // ��ҿ�����Ҫ����ĳЩ״̬
        docombo = false;

        // ���ù���Ŀ��
        currTarget = target;

        Debug.Log($"���׼������: {(target != null ? target.gameObject.name : "��Ŀ��")}");
    }


    // ��ҹ��������߼�
    public void FinishPlayerAttack()
    {
        // ��ҿ�����Ҫ�ڹ�������������״̬��
        Debug.Log("��ҹ�������");
    }


    // ���ר��״̬����
    public void UpdatePlayerAttackState(float normalizedTime, AttackData attack)
    {
        
        if (Attackstate == AttackStates.Windup)
        {
            if (normalizedTime >= attack.ImpactStartTime)
            {
                Attackstate = AttackStates.Impact;
                Debug.Log("��ҹ�������Impact״̬");

                EnablePlayerHitbox(attack);
            }
        }
        else if (Attackstate == AttackStates.Impact)
        {
            if (normalizedTime >= attack.ImpactEndTime)
            {
                Attackstate = AttackStates.Cooldown;
                Debug.Log("��ҹ�������Cooldown״̬");
                DisablePlayerHitboxes();
            }
        }
    }


    // ���״̬����
    public void ResetPlayerAttackState()
    {
        Attackstate = AttackStates.Idle;
        InAction = false;
        comboCount = 0;
        docombo = false;
        Debug.Log("��ҹ���״̬����");
    }


    // �������״̬���
    public bool CheckPlayerComboCondition()
    {
        
        return docombo &&
               (Attackstate == AttackStates.Impact ||
                Attackstate == AttackStates.Cooldown);
    }


    // ���ר��Hitbox����
    public void EnablePlayerHitbox(AttackData attack)
    {
        hitTargets.Clear();

        switch (attack.HitboxToUse)
        {
            case AttackHitbox.LeftHand:
                if (leftHandCollider != null)
                {
                    leftHandCollider.enabled = true;
                    Debug.Log("�����������Hitbox");
                }
                break;
            case AttackHitbox.RightHand:
                if (rightHandCollider != null)
                {
                    rightHandCollider.enabled = true;
                    Debug.Log("�����������Hitbox");
                }
                break;
            case AttackHitbox.LeftFoot:
                if (leftFootCollider != null)
                {
                    leftFootCollider.enabled = true;
                    Debug.Log("����������Hitbox");
                }
                break;
            case AttackHitbox.RightFoot:
                if (rightFootCollider != null)
                {
                    rightFootCollider.enabled = true;
                    Debug.Log("��������ҽ�Hitbox");
                }
                break;
            case AttackHitbox.Sword:
                // ���ʹ��װ����������ȡ������ײ��
                var weaponCollider = WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.GetComponentInChildren<BoxCollider>();
                if (weaponCollider != null)
                {
                    weaponCollider.enabled = true;
                    Debug.Log("�����������Hitbox");
                }
                else
                {
                    Debug.LogWarning("���������ײ��Ϊnull���޷�����");
                }
                break;
            default:
                Debug.Log($"���ʹ��δ֪Hitbox����: {attack.HitboxToUse}");
                break;
        }
    }


    // ���ר��Hitbox����
    public void DisablePlayerHitboxes()
    {
        // �����������岿λHitbox
        if (leftHandCollider != null)
            leftHandCollider.enabled = false;
        if (rightHandCollider != null)
            rightHandCollider.enabled = false;
        if (leftFootCollider != null)
            leftFootCollider.enabled = false;
        if (rightFootCollider != null)
            rightFootCollider.enabled = false;

        // ��������Hitbox
        var weaponCollider = WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.GetComponentInChildren<BoxCollider>();
        if (weaponCollider != null)
            weaponCollider.enabled = false;

        CurrentSpecialHitReaction = null;

        Debug.Log("�����������Hitbox");
    }

    public IEnumerator ExecutePlayerAttack(ICombatSystem target, int comboCount)
    {
        // 1. ׼������
        PreparePlayerAttack(target);

       
        InAction = true;
        currTarget = target;
        Attackstate = AttackStates.Windup;

        // 2. ��ȡ��������
        var attack = SelectPlayerAttack(target,Attacks, LongRangeAttacks, comboCount, LongRangeAttackThreshold);
        Vector3 attackDir = CalculatePlayerAttackDirection(target);
        Vector3 startPos = transform.position;
        Vector3 targetPos = CalculatePlayerAttackPosition(target, attack, attackDir, startPos);

        // 3. ���Ŷ���
        animator.CrossFade(attack.AttackName, 0.2f);
        yield return null;
        var animstate = animator.GetNextAnimatorStateInfo(1);

        // 4. ����ִ��ѭ��
        float timer = 0f;
        while (timer <= animstate.length)
        {
            if (IsTakingHit) break;

            timer += Time.deltaTime;
            float normalizedTime = timer / animstate.length;

            // �ƶ��߼�
            if (target != null && attack.MoveToTarget)
            {
                float percTime = (normalizedTime - attack.MoveStartTime) / (attack.MoveEndTime - attack.MoveStartTime);
                Vector3 desiredPosition = Vector3.Lerp(startPos, targetPos, percTime);
                Vector3 moveDelta = desiredPosition - transform.position;

                CharacterController controller = GetComponent<CharacterController>();
                if (controller != null)
                {
                    controller.Move(moveDelta);
                }
                else
                {
                    transform.position = desiredPosition;
                }
            }

            // ת�����
            if (attackDir != Vector3.zero)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(attackDir),
                    500f * Time.deltaTime);
            }

            // 5. ״̬����
            UpdatePlayerAttackState(normalizedTime, attack);

            // 6. �������
            if (CheckPlayerComboCondition())
            {
                docombo = false;
                int newComboCount = (comboCount + 1) % Attacks.Count;
                StartCoroutine(ExecutePlayerAttack(target, newComboCount));
                yield break;
            }

            yield return null;
        }

        // 7. ��������
        ResetPlayerAttackState();
        FinishPlayerAttack();

        currTarget = null;
    }




    #region ICombatSystem�ӿڷ���ʵ�ֽӿڷ���ʵ��

    public bool HasUsableWeapon() => PlayerHasUsableWeapon();//HasUsebleWeapon�ӿ�ʵ��
    public bool CanAttack() => PlayerCanAttack();//CanAttack�ӿ�ʵ��
    public void TryToAttack(ICombatSystem target = null) => PlayerTryToAttack(target);//TryAttack�ӿ�ʵ��
    public Vector3 CalculateAttackPosition(ICombatSystem target, AttackData attack, Vector3 attackDir, Vector3 startPos)
      => CalculatePlayerAttackPosition(target, attack, attackDir, startPos);//CalculateAttackPosition�ӿ�ʵ��
    public Vector3 CalculateAttackDirection(ICombatSystem target) => CalculatePlayerAttackDirection(target);//CalculateAttackDirection�ӿ�ʵ��
    public void PrepareAttack(ICombatSystem target) => PreparePlayerAttack(target);// PrepareAttack�ӿ�ʵ��
    public void FinishAttack() => FinishPlayerAttack();
    public void UpdateAttackState(float normalizedTime, AttackData attack) => UpdatePlayerAttackState(normalizedTime, attack);
    public void ResetAttackState() => ResetPlayerAttackState();
    public bool CheckComboCondition() => CheckPlayerComboCondition();//CheckComboCondition�ӿ�ʵ��
    public void EnableHitbox(AttackData attack) => EnablePlayerHitbox(attack);
    public void DisableHitboxes() => DisablePlayerHitboxes();
    public AttackData SelectAttack(ICombatSystem target, int comboCount)
    {
        return SelectPlayerAttack(target, Attacks, LongRangeAttacks, comboCount, LongRangeAttackThreshold);
    }
    public IEnumerator ExecuteAttack(ICombatSystem target, int comboCount)
    {
        yield return ExecutePlayerAttack(target, comboCount);
    }

    public bool IsUsingHeavyWeapon()
    {
        return WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.isHeavy ?? false;
    }

    Transform ICombatSystem.transform => this.transform;
    GameObject ICombatSystem.gameObject => this.gameObject;
    
    #endregion
}

