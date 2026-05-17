using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MapIcon : MonoBehaviour
{
   private MapEntity _entity;
   public MapEntity Entity=>_entity;
   private Image _image;
   private RectTransform _rectTransform;

   private bool _rotateWithTagret;
   private float _rotateSpeed;
    void Awake()
    {
        _image=GetComponent<Image>();
        _rectTransform=GetComponent<RectTransform>();
    }

    public void Init(MapEntity mapEntity)
    {
        _entity=mapEntity;
        _image.sprite=_entity.Data.Category.CategoryIcon;
        _image.color=_entity.Data.Category.CategoryColor;
        _rotateWithTagret=_entity.Data.RotateWithTarget;
        _rotateSpeed=_entity.Data.RoatateSpeed;
        
    }
    public void SetPosition(Vector2 position)
    {
        _rectTransform.anchoredPosition=position;
    }
    void Update()
    {
        if (_rotateWithTagret)
        {
            var entityRotation=_entity.transform.eulerAngles.y;
            var targetRotation=Quaternion.Euler(0.0f,0.0f,-entityRotation);

            _rectTransform.localRotation=Quaternion.RotateTowards(_rectTransform.localRotation,targetRotation,_rotateSpeed*Time.deltaTime);
        }
    }
}
