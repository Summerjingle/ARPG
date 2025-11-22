using UnityEngine;

public class PositionSnitch : MonoBehaviour
{
    Vector3 lastPos;

    void LateUpdate()
    {
        if (transform.position != lastPos)
        {
            Debug.LogWarning(
                $"[Snitch] {gameObject.name} moved! NewPos: {transform.position}\n" +
                $"CallStack:\n{new System.Diagnostics.StackTrace()}"
            );
        }

        lastPos = transform.position;
    }
}
