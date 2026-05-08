using UnityEngine;

public class EventTriggerManager : MonoBehaviour
{
    private static EventTriggerManager _instance;

    public static EventTriggerManager Get()
    {
        if (_instance == null)
            _instance = GameObject.FindAnyObjectByType<EventTriggerManager>();
        return _instance;
    }

    public enum TriggerEventType { None, ForwardHeadPosture, HeadYawDeviation, Shrug };

    private TriggerEventType _currTriggerEvent = TriggerEventType.None;

    private bool _isCooling = false;

    // 触发事件
    public void EventTrigger(TriggerEventType type)
    {
        if (_isCooling == true) return;

        _isCooling = true;
        _currTriggerEvent = type;
        ITriggerEvent trigger;
        switch (type)
        {
            case TriggerEventType.ForwardHeadPosture:
                trigger = TryGetTriggerEvent<FHPEvent>();
                trigger.OnEvent(() => _isCooling = false);
                break;
            case TriggerEventType.HeadYawDeviation:
                trigger = TryGetTriggerEvent<HYDEvent>();
                trigger.OnEvent(() => _isCooling = false);
                break;
            case TriggerEventType.Shrug:
                trigger = TryGetTriggerEvent<ShrugEvent>();
                trigger.OnEvent(() => _isCooling = false);
                break;
        }
    }

    // —— 工具方法 ——

    /// <summary>
    /// 获取合适的触发事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ITriggerEvent TryGetTriggerEvent<T>() where T : MonoBehaviour, ITriggerEvent
    {
        T trigger = default;

        trigger = gameObject.GetComponent<T>();
        if (trigger == null) trigger = gameObject.AddComponent<T>();

        return trigger;
    }
}
