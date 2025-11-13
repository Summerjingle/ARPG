using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Parkour System/New parkour action")]
public class ParkourAction : ScriptableObject
{
    [SerializeField] private string animName;
    [SerializeField] private float minHeight;
    [SerializeField] private float maxHeight;

    [SerializeField] private bool rotateToObstacle;
    [SerializeField] private float postActionDelay;

    [Header("自动匹配障碍物高度")]
    [SerializeField] private bool enableTargetMatching=true;
    [SerializeField] private AvatarTarget matchBodyPart;
    [SerializeField] private float macthStartTime;
    [SerializeField] private float macthTargetTime;
    [SerializeField] private Vector3 matchPosWight = new Vector3(0, 1, 0);

    public Quaternion TargetRotation { get; set; }
    public Vector3 MatchPos {  get; set; }

    public bool CheckIfPossible(ObstacleData hitData,Transform player)
    {
         float height= hitData.heightHit.point.y - player.position.y;
        if (height<minHeight || height>maxHeight)
            return false;
        if (rotateToObstacle)
            TargetRotation=Quaternion.LookRotation(-hitData.forwardHit.normal);
        if (enableTargetMatching)
            MatchPos=hitData.heightHit.point;
            return true;
        
    }

    public string AnimName => animName;
    public bool RotateToObstacle => rotateToObstacle;

    public bool EnableTargetMatching => enableTargetMatching;
    public AvatarTarget MatchBodyPart=>matchBodyPart;
    public float MacthStartTime=>macthStartTime;
    public float MacthTargetTime => macthTargetTime;
    public Vector3 MatchPosWight => matchPosWight;
    public float PostActionDelay=>postActionDelay;
}
