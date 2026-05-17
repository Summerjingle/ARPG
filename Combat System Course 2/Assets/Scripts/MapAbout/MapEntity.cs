using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapEntity : MonoBehaviour
{
    [SerializeField]private MapEntityData data;
    public MapEntityData Data=>data;
    private MapIcon _icon;

    private void Start()
    {
        _icon=MapManager.Instance.RegisterMapEntity(this);
    }
    void OnDisable()
    {
        if(_icon!=null)
            MapManager.Instance.UnregisterMapEntity(_icon);
    }
}
