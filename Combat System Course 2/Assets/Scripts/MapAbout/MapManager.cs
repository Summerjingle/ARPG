using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;
    [SerializeField]private Transform target;
    
    [SerializeField]private GameObject iconPrefab;
    [SerializeField]private Transform iconParent;
    [SerializeField]private RectTransform mapImage;
    [SerializeField]private Transform boundsMin;
    [SerializeField]private Transform boundsMax;
    
    private List<MapIcon>_icons=new();
    void Awake()
    {
        if (Instance == null)
        {
            Instance=this;
            //DontDestroyOnLoad(gameObject);暂时不考虑
        }
        else
        {
            //Destroy(gameObject);
        }
    }

    private void Update()
    {
        var mapPosition=WorldToMapPosition(target.position);
        mapImage.anchoredPosition=-mapPosition;
        foreach(var icon in _icons)
        {
            var position=WorldToMapPosition(icon.Entity.transform.position);
            icon.SetPosition(position);
        }
    }
    public MapIcon RegisterMapEntity(MapEntity mapEntity)
    {
        var icon=Instantiate(iconPrefab,iconParent);
        if(!icon.TryGetComponent<MapIcon>(out var mapIcon))
        {
            Destroy(icon);
            return null;
        }
        icon.name=$"Icon{mapEntity.name}";
        mapIcon.Init(mapEntity);
        _icons.Add(mapIcon);
        return mapIcon;
    }
    public void UnregisterMapEntity(MapIcon mapIcon)
    {
        _icons.Remove(mapIcon);
    }
    Vector2 WorldToMapPosition(Vector3 worldPosition)
    {
        var normalizedX=Mathf.InverseLerp(boundsMin.position.x,boundsMax.position.x,worldPosition.x);
        var normalizedY=Mathf.InverseLerp(boundsMin.position.z,boundsMax.position.z,worldPosition.z);
        var mapX=normalizedX*mapImage.sizeDelta.x-mapImage.sizeDelta.x/2.0f;
        var mapY=normalizedY*mapImage.sizeDelta.y-mapImage.sizeDelta.y/2.0f;
        return new Vector2(mapX,mapY);


    }
}
