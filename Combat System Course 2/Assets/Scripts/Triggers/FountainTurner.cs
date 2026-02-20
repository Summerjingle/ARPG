using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FountainDirection
{
    Forward, // 0
    Right,   // 90
    Back,    // 180
    Left     // 270
}
public class FountainTurner : MonoBehaviour
{
    public float rotateSpeed = 90f;
    private bool isRotating = false;

    private float targetYAngle;
    public FountainDirection currentDirection;
    private FountainDirection targetDirection;

    public event System.Action<FountainDirection> OnRotationFinished;
    private void Start()
    {
        
        currentDirection = FountainDirection.Forward;
        targetDirection = currentDirection;

        targetYAngle = 0;
        transform.rotation = Quaternion.Euler(0, targetYAngle, 0);

        
        OnRotationFinished?.Invoke(currentDirection);

    }

    void Update()
    {
        if (!isRotating)
            return;

        float currentY = transform.eulerAngles.y;

        float newY = Mathf.MoveTowardsAngle(
            currentY,
            targetYAngle,
            rotateSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Euler(0, newY, 0);

        if (Mathf.Abs(Mathf.DeltaAngle(newY, targetYAngle)) < 0.1f)
        {
            transform.rotation = Quaternion.Euler(0, targetYAngle, 0);

            currentDirection = targetDirection;
            isRotating = false;

            OnRotationFinished?.Invoke(currentDirection);
        }
    }

    public void SetDirection(FountainDirection dir)
    {
        if (currentDirection == dir)
            return;

        targetDirection = dir;
        isRotating = true;

        switch (dir)
        {
            case FountainDirection.Forward: targetYAngle = 0; break;
            case FountainDirection.Right: targetYAngle = 90; break;
            case FountainDirection.Back: targetYAngle = 180; break;
            case FountainDirection.Left: targetYAngle = 270; break;
        }
    }

    public bool IsRotating => isRotating;
}

