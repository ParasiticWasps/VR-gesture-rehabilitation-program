using UnityEngine;
using UnityEngine.XR;

public class HeadForwardDetection : MonoBehaviour
{
    private static HeadForwardDetection _instance;
    public static HeadForwardDetection Get()
    {
        if (_instance == null )
            _instance = GameObject.FindAnyObjectByType<HeadForwardDetection>();
        return _instance;
    }

    [Tooltip("允许头部前移的最大距离（米）")]
    public float maxForwardDistance = 0.08f; // 8cm

    public float currentForwardDistance { get; private set; } = 0f; // 单位cm

    private Vector3 initialHeadLocalPosition;

    private bool _canRecord = true;

    private void Record()
    {
        if (_canRecord == true)
        {
            var inputDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (inputDevice.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 pos))
            {
                initialHeadLocalPosition = pos;
            }
            _canRecord = false;
        }
    }

    void Update()
    {
        if (HandGestureMove.Get().isMoving == true)
        {
            _canRecord = true;
            return;
        }

        Record();
        var inputDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (inputDevice.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 currentPos))
        {
            // 计算相对于初始位置的偏移量
            Vector3 delta = currentPos - initialHeadLocalPosition;
            currentForwardDistance = delta.z * 100.0f;
            UIManager.Get().SetForwardDistanceText(Mathf.Abs(currentForwardDistance).ToString("F0"));

            if (currentForwardDistance > maxForwardDistance || currentForwardDistance < (maxForwardDistance * -1.0f))
            {
                EventTriggerManager.Get().EventTrigger(EventTriggerManager.TriggerEventType.ForwardHeadPosture);
            }
        }
    }
}