using UnityEngine;

public class VRUIFollow : MonoBehaviour
{
    public float distance = 2f;           // 与摄像机的距离
    public float horizontalOffset = -0.3f; // 水平偏移：正右负左
    public float verticalOffset = 0.2f;    // 垂直偏移：正上负下
    public float smoothSpeed = 10f;        // 平滑跟随速度

    void LateUpdate()
    {
        Transform cam = Camera.main.transform;

        // 目标位置：摄像机前方 + 左偏移 + 上偏移
        Vector3 targetPos = cam.position
                          + cam.forward * distance
                          + cam.right * horizontalOffset
                          + cam.up * verticalOffset;

        Quaternion targetRot = Quaternion.LookRotation(cam.forward, Vector3.up);

        // 指数平滑，帧率无关
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
    }
}