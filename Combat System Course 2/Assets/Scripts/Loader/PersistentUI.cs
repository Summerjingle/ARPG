using UnityEngine;

public class PersistentUI : MonoBehaviour
{
    private static PersistentUI instance;

    private void Awake()
    {
        // 单例模式检查
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}