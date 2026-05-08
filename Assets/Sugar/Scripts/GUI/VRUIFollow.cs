using UnityEngine;

public class VRUIFollow : MonoBehaviour
{
    public float distance = 2f;         // 与摄像机的距离
    public float verticalOffset = 0f;   // 设为0即正中心（原为-0.5下方）
    public float smoothSpeed = 10f;     // 平滑速度

    void LateUpdate()
    {
        Transform cam = Camera.main.transform;

        // 目标位置：正前方，无垂直偏移（也可保留字段以便微调）
        Vector3 targetPos = cam.position + cam.forward * distance + cam.up * verticalOffset;
        Quaternion targetRot = Quaternion.LookRotation(cam.forward, Vector3.up);

        // 指数平滑移动和旋转
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
    }
}