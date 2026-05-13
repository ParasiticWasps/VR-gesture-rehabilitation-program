using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局管理器组，带着子管理器场景切换不销毁。
/// </summary>
public class GlobalManagers : MonoBehaviour
{
    private static GlobalManagers _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
