using UnityEngine;
using UnityEngine.XR; // 引入XR库

[RequireComponent(typeof(CharacterController))]
public class HandLocomotion : MonoBehaviour
{
    [Header("设置")]
    public float moveSpeed = 1.5f; // 移动速度
    public float turnSpeed = 45f;  // 转身速度
    public Transform headCamera;   // 你的头（摄像机）

    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        // 如果没手动拖拽，自动找 Main Camera
        if (headCamera == null) headCamera = Camera.main.transform;
    }

    void Update()
    {
        // 1. 获取左手数据
        var leftHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!leftHandDevice.isValid) return;

        // 2. 检测手势
        // PICO 手势追踪中，"Pinch" (捏合) 通常映射为 Trigger (扳机键)
        // "Grip" (握拳) 通常映射为 Grip (侧键)
        bool isPinching = false;
        bool isGripping = false;

        // 获取捏合力度 (0~1)
        if (leftHandDevice.TryGetFeatureValue(CommonUsages.trigger, out float pinchValue))
            isPinching = pinchValue > 0.8f; // 捏紧一点才算

        // 获取握拳力度 (0~1)
        if (leftHandDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
            isGripping = gripValue > 0.8f;

        // 3. 执行移动 (捏合 = 前进)
        if (isPinching)
        {
            // 获取头部朝向，但要忽略 Y 轴 (防止看天时飞起来，看地时钻地)
            Vector3 forward = headCamera.forward;
            forward.y = 0;
            forward.Normalize();

            // 移动
            characterController.Move(forward * moveSpeed * Time.deltaTime);
        }

        // 4. 执行转向 (握拳 = 向右转) -> 可选功能
        if (isGripping)
        {
            // 绕着 Y 轴旋转
            transform.Rotate(0, turnSpeed * Time.deltaTime, 0);
        }

        // 5. 模拟重力 (防止浮空)
        // 简单的重力模拟，让人贴在地上
        characterController.Move(Vector3.down * 9.8f * Time.deltaTime);
    }
}