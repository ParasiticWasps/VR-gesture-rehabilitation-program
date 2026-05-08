using System;
using System.Collections;
using UnityEngine;

public class ShrugEvent : MonoBehaviour, ITriggerEvent
{
    public void OnEvent(Action callback)
    {
        StartCoroutine(OnEventCoroutine(callback));
    }

    private IEnumerator OnEventCoroutine(Action callback)
    {
        UIManager.Get().SetWarningText("警告！过度耸肩警告！");

        yield return new WaitForSeconds(2.0f);

        UIManager.Get().SetWarningText("");
        callback?.Invoke();
    }
}