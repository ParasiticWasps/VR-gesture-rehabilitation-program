using System;
using System.Collections;
using UnityEngine;

public class HYDEvent : MonoBehaviour, ITriggerEvent
{
    private const string AUDIO_PATH = "Sound/HYDWarning";

    private AudioClip _currClip;

    public void OnEvent(Action callback)
    {
        StartCoroutine(OnEventCoroutine(callback));
    }

    private IEnumerator OnEventCoroutine(Action callback)
    {
        // 播放警告音频
        if (_currClip == null) _currClip = AudioManager.Get().Play(AUDIO_PATH);
        else AudioManager.Get().Play(_currClip);

        // 显示警告文本提示
        UIManager.Get().SetWarningText("警告！躯干过度旋转！");

        yield return new WaitForSeconds(2.0f);

        UIManager.Get().SetWarningText("");
        callback?.Invoke();
    }    
}