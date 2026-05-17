using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="MapEntityData",menuName ="MiniMap/EntityData")]

public class MapEntityData : ScriptableObject
{
    [SerializeField]private MapCategory category;
    public MapCategory Category=>category;
    [SerializeField]private bool rotateWithTagret;
    public bool RotateWithTarget=>rotateWithTagret;
    [SerializeField]private float rotateSpeed;
    public float RoatateSpeed=>rotateSpeed;
}
