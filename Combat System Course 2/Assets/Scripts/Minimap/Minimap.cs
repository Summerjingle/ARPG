using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Minimap : MonoBehaviour
{
    public Transform player;
    public GameObject playerMarker;
    private void LateUpdate()
    {
        Vector3 newPosition=player.position;
        newPosition.y = transform.position.y;
        transform.position = newPosition;

        playerMarker.transform.rotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y - 180f);
    }
}
