using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    // Start is called before the first frame update
    public ElevatorController triggerController; // Ö¸Ïò°´Å¥
    public void OnElevatorEnd()
    {
        if (triggerController != null)
            triggerController.ElevatorFinished();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
