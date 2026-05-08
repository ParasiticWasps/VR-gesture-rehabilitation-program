using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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

    [Tooltip("允许手高于头部的最大垂直距离（CM）")]
    public float maxHeightDifference = 3f;

    [Tooltip("从这里拖入左手模型/空节点")]
    public Transform leftHand;

    [Tooltip("从这里拖入右手模型/空节点")]
    public Transform rightHand;

    public Transform head;

    private float _leftDifference = 0f;
    private float _rightDifference = 0f;

    void Update()
    {
        // 1. 获取头部世界位置
        //var headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        //if (!headDevice.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 headPos))
        //    return;

        bool triggered = false;

        // 2. 检查左右手是否高于头部
        if (leftHand != null)
        {
            _leftDifference = (leftHand.position.y - head.position.y) * 100.0f;
            triggered |= _leftDifference > maxHeightDifference;
        }

        if (rightHand != null)
        {
            _rightDifference = (rightHand.position.y - head.position.y) * 100.0f;
            triggered |= _rightDifference > maxHeightDifference;
        }

        //UIManager.Get().AddLogTextContent($"h: {head.position.y.ToString("F2")}, r: {rightHand.position.y.ToString("F2")}, d:{_rightDifference.ToString("F2")}");

        float _maxYDifference  = Mathf.Max(_leftDifference, _rightDifference);
        _leftDifference  = 0f;
        _rightDifference = 0f;

        UIManager.Get().SetShrugDistanceText(_maxYDifference.ToString("F0"));

        // 3. 触发事件
        if (triggered)
            EventTriggerManager.Get()?.EventTrigger(EventTriggerManager.TriggerEventType.Shrug);
    }
}