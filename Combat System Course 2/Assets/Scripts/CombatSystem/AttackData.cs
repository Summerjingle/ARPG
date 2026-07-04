using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Combat System/Create a new attack")]


public class AttackData : ScriptableObject
{
	[field:SerializeField] public string AttackName {  get; private	 set; }
	[field:SerializeField] public AttackHitbox HitboxToUse {  get; private	 set; }
	[field:SerializeField] public float ImpactStartTime {  get; private	 set; }
	[field:SerializeField] public float ImpactEndTime {  get; private	 set; }

	[field: Header("�ƶ���������")]
	[field:SerializeField] public bool MoveToTarget{  get; private	 set; }
	[field: SerializeField] public float DistanceFromTarget { get; private set; } = 1f;
	[field: SerializeField] public float MaxMoveDistance { get; private set; } = 3f;


	[field: SerializeField] public float MoveStartTime { get; private set; } = 0f;
	[field: SerializeField] public float MoveEndTime { get; private set; } = 1f;

	[field: Header("伤害")]
	[field: SerializeField] public float Damage { get; private set; } = 10f;

	[field: Header("受击反应")]
	[field: SerializeField] public string SpecialHitReaction { get; private set; }
	[field: SerializeField] public bool IsKnockdown { get; private set; } = false;

	[field: Header("特殊标记")]
	[field: SerializeField] public bool IsSpinAttack { get; private set; } = false;

}
public enum AttackHitbox { LeftHand, RightHand, LeftFoot, RightFoot, Sword, BothHands, BothFeet, Body }
