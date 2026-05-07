using UnityEngine;
using UnityEngine.XR;

public class ShrugDetection : MonoBehaviour
{
    private static ShrugDetection _instance;
    public static ShrugDetection Get()
    {
        if (_instance == null)
            _instance = FindAnyObjectByType<ShrugDetection>();
        return _instance;
    }

    [Tooltip("手高于头部的垂直阈值（米），默认3厘米")]
    public float maxHeightDifference = 0.03f;

    [Tooltip("当前手超过头部的最大垂直高度（厘米）")]
    public float currentMaxHandAboveHead { get; private set; } = 0f;

    void Update()
    {
        // 获取头部位置
        var headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (!headDevice.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 headPos))
            return; // 头部追踪不可用则跳过

        float maxAbove = 0f;
        bool handAbove = false;

        // 检测左手
        CheckHand(XRNode.LeftHand, headPos, ref maxAbove, ref handAbove);
        // 检测右手
        CheckHand(XRNode.RightHand, headPos, ref maxAbove, ref handAbove);

        // 转换为厘米
        currentMaxHandAboveHead = maxAbove * 100f;

        // 显示在UI上（可选）
        if (UIManager.Get() != null)
            UIManager.Get().SetShrugDistanceText(currentMaxHandAboveHead.ToString("F0"));

        if (handAbove)
        {
            // 触发手过高事件
            EventTriggerManager.Get()?.EventTrigger(EventTriggerManager.TriggerEventType.Shrug);
        }
    }

    private void CheckHand(XRNode handNode, Vector3 headPos, ref float maxAbove, ref bool triggered)
    {
        var handDevice = InputDevices.GetDeviceAtXRNode(handNode);
        if (handDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 handPos))
        {
            float diff = handPos.y - headPos.y;
            if (diff > maxAbove)
                maxAbove = diff;

            if (diff > maxHeightDifference)
                triggered = true;
        }
    }
}