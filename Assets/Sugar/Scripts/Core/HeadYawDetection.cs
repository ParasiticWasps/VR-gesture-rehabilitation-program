using UnityEngine;
using UnityEngine.XR;

public class HeadYawDetection : MonoBehaviour
{
    private static HeadYawDetection _instance;
    public static HeadYawDetection Get()
    {
        if (_instance == null)
            _instance = FindAnyObjectByType<HeadYawDetection>();
        return _instance;
    }

    [Tooltip("允许头部偏转的最大角度（度）")]
    public float maxYawAngle = 15f;

    [Tooltip("当前偏转角度（度），0表示无偏转")]
    public float currentYawOffset { get; private set; } = 0f;

    // 基准 yaw 角（世界坐标系，0~360）
    private float referenceYaw = 0f;
    private bool _canRecord = true;

    void Start()
    {
        // 初始记录可以放在这里或首次Update
    }

    /// <summary>
    /// 记录当前头部朝向作为新的基准
    /// </summary>
    private void Record()
    {
        if (_canRecord)
        {
            var inputDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (inputDevice.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion rot))
            {
                // 获取绕世界Y轴的角度
                referenceYaw = rot.eulerAngles.y;
            }
            _canRecord = false;
        }
    }

    void Update()
    {
        // 如果正在手柄移动，允许重新记录基准朝向
        if (HandGestureMove.Get().isMoving == true)
        {
            _canRecord = true;
            return;
        }

        // 至少记录一次基准
        Record();

        var inputDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (inputDevice.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion currentRot))
        {
            float currentYaw = currentRot.eulerAngles.y;

            // 计算与基准 yaw 的最小角度差（范围 -180 ~ 180）
            float delta = Mathf.DeltaAngle(referenceYaw, currentYaw);
            currentYawOffset = Mathf.Abs(delta); // 取绝对值用做UI显示

            // 在UI上显示偏转角度（可选）
            if (UIManager.Get() != null)
                UIManager.Get().SetYawDistanceText(currentYawOffset.ToString("F0"));

            if (currentYawOffset > maxYawAngle)
            {
                // 触发偏头事件，供其他系统处理
                EventTriggerManager.Get()?.EventTrigger(EventTriggerManager.TriggerEventType.HeadYawDeviation);
            }
        }
    }
}