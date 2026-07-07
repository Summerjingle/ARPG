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

	[field: Header("移动参数")]
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

	[field: Header("摄像机震动")]
	[field: SerializeField] public bool EnableCameraShake { get; private set; } = false;
	[field: SerializeField] public float CameraShakeIntensity { get; private set; } = 0.5f;
	[field: SerializeField] public float CameraShakeDuration { get; private set; } = 0.2f;
	[field: SerializeField] public float CameraShakeFrequency { get; private set; } = 10f;
	[field: SerializeField] public float CameraShakeTime { get; private set; } = 0f;

	[field: Header("攻击特效")]
	[field: SerializeField] public GameObject AttackVFXPrefab { get; private set; }
	[field: SerializeField] public float VFXSpawnTime { get; private set; } = 0f;
	[field: SerializeField] public bool VFXFollowAttacker { get; private set; } = false;
	[field: SerializeField] public Vector3 VFXSpawnOffset { get; private set; } = Vector3.zero;

	[field: Header("攻击音效")]
	[field: SerializeField] public AudioClip AttackSFX { get; private set; }
	[field: SerializeField] public float SFXSpawnTime { get; private set; } = 0f;

}
public enum AttackHitbox { LeftHand, RightHand, LeftFoot, RightFoot, Sword, BothHands, BothFeet, Body }
