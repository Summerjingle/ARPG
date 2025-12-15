using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MessageUI.Instance.Show("你获得了一点经验");
            Destroy(gameObject);
        }
    }
}
