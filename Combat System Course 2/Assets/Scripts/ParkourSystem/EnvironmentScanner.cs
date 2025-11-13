using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentScanner : MonoBehaviour
{
    [SerializeField] Vector3 forwardRayOffset = new Vector3(0.2f, 0.3f, 0);
    [SerializeField] float forwardRayLength = 0.8f;
    [SerializeField] float heightRayLength = 5f;
    [SerializeField] LayerMask obstacleLayer;
    public ObstacleData ObstacleCheck()
    {
        var hitData=new ObstacleData();
        var forwardOrigin = transform.position + forwardRayOffset;//射线起点：玩家膝盖处=玩家位置+y轴加一点高度
        hitData.forwardHitFound=Physics.Raycast(forwardOrigin, transform.forward, out hitData.forwardHit, forwardRayLength, obstacleLayer);
        Debug.DrawRay(forwardOrigin, transform.forward * forwardRayLength, hitData.forwardHitFound ? Color.red : Color.white); 
        if (hitData.forwardHitFound)
        {
            var heightOrigin = hitData.forwardHit.point + Vector3.up * heightRayLength;//射线起点
            hitData.heightHitFound=Physics.Raycast(heightOrigin,Vector3.down,out hitData.heightHit,heightRayLength,obstacleLayer);
            Debug.DrawRay(heightOrigin,Vector3.down*heightRayLength,hitData.heightHitFound?Color.red:Color.white);
        }
        
        return hitData;
    }
}
public struct ObstacleData
{
    public bool forwardHitFound;
    public bool heightHitFound;
    public RaycastHit forwardHit;
    public RaycastHit heightHit;
}
