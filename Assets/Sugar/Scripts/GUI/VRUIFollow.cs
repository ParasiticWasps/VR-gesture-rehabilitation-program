using UnityEngine;

public class VRUIFollow : MonoBehaviour
{
    public float distance = 2f;
    public float verticalOffset = -0.5f;
    public float smoothSpeed = 10f;

    void LateUpdate()
    {
        Transform cam = Camera.main.transform;
        Vector3 targetPos = cam.position + cam.forward * distance + cam.up * verticalOffset;
        Quaternion targetRot = Quaternion.LookRotation(cam.forward, Vector3.up);
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
    }
}