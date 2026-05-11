using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂在桌面上。摩擦时直接降低 Shader 的 _DirtAmount 值。
/// 从 1（全脏）擦到 0（全干净），简单直接。
/// </summary>
public class WipeSurface : MonoBehaviour
{
    [Header("擦拭速度")]
    [Tooltip("每秒擦掉多少脏度（基础值）")]
    public float wipeSpeed = 0.05f;

    [Tooltip("移动越快擦得越快的倍率")]
    public float speedMultiplier = 3f;

    [Tooltip("最小移动速度才算擦拭")]
    public float minWipeSpeed = 0.01f;

    [Header("只读")]
    public float currentDirtAmount = 1f;

    private Material material;
    private Dictionary<Transform, Vector3> lastPositions = new();

    public Action<float> OnWipe;

    private void Start()
    {
        material = GetComponent<Renderer>().material;
        currentDirtAmount = material.GetFloat("_DirtAmount");
    }

    private void OnCollisionStay(Collision collision)
    {
        var wiper = collision.collider.GetComponentInParent<ClothWiper>();
        if (wiper == null || !wiper.IsGrabbed) return;

        Transform wiperTransform = wiper.transform;
        Vector3 currentPos = wiperTransform.position;

        if (!lastPositions.ContainsKey(wiperTransform))
        {
            lastPositions[wiperTransform] = currentPos;
            return;
        }

        Vector3 lastPos = lastPositions[wiperTransform];
        float speed = Vector3.Distance(currentPos, lastPos) / Time.fixedDeltaTime;
        lastPositions[wiperTransform] = currentPos;

        // 不动不算擦
        if (speed < minWipeSpeed) return;

        // 速度越快擦得越快
        float wipeAmount = wipeSpeed * Time.fixedDeltaTime * Mathf.Clamp(speed * speedMultiplier, 0.5f, 5f);

        currentDirtAmount = Mathf.Max(0f, currentDirtAmount - wipeAmount);
        material.SetFloat("_DirtAmount", currentDirtAmount);
        OnWipe?.Invoke(currentDirtAmount - wipeAmount);
    }

    private void OnCollisionExit(Collision collision)
    {
        var wiper = collision.collider.GetComponentInParent<ClothWiper>();
        if (wiper != null)
            lastPositions.Remove(wiper.transform);
    }

    /// <summary>
    /// 重置为全脏
    /// </summary>
    public void ResetDirt()
    {
        currentDirtAmount = 1f;
        material.SetFloat("_DirtAmount", currentDirtAmount);
    }

    private void OnDestroy()
    {
        if (material) Destroy(material);
    }
}