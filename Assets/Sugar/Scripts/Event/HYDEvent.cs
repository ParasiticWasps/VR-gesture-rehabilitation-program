using System;
using System.Collections;
using UnityEngine;

public class HYDEvent : MonoBehaviour, ITriggerEvent
{
    public void OnEvent(Action callback)
    {
        StartCoroutine(OnEventCoroutine(callback));
    }

    private IEnumerator OnEventCoroutine(Action callback)
    {
        UIManager.Get().SetWarningText("警告！躯干过度旋转！");

        yield return new WaitForSeconds(2.0f);

        UIManager.Get().SetWarningText("");
        callback?.Invoke();
    }    
}