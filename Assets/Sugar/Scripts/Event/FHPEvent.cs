using System;
using System.Collections;
using UnityEngine;

public class FHPEvent : MonoBehaviour, ITriggerEvent
{
    public void OnEvent(Action callback)
    {
        StartCoroutine(OnEventCoroutine(callback));
    }

    private IEnumerator OnEventCoroutine(Action callback)
    {
        UIManager.Get().SetWarningText("警告！身体前倾！");

        yield return new WaitForSeconds(2.0f);

        UIManager.Get().SetWarningText("");
        callback?.Invoke();
    }    
}