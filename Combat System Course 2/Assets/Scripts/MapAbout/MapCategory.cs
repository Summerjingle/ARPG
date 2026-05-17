using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="MapCategory",menuName ="MiniMap/MapCategory")]
public class MapCategory : ScriptableObject
{
   [SerializeField]private Sprite categoryIcon;
    public Sprite CategoryIcon=>categoryIcon;
   [SerializeField]private Color categoryColor;//决定图标颜色
    public Color CategoryColor=>categoryColor;    


}
