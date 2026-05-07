using UnityEngine;
using System.Collections.Generic;

public class MirrorHandDriver : MonoBehaviour
{
    [Header("核心引用")]
    public Transform realHandRoot;   // 真实的左手 (Hand_L)
    public Transform mirrorHandRoot; // 镜像的右手 (Hand_R)

    [Header("平滑参数")]
    [Range(0, 1)] public float smoothFactor = 0.5f; // 值越大越平滑，但延迟越高

    // 缓存骨骼对应关系
    private List<(Transform real, Transform mirror)> bonePairs = new List<(Transform, Transform)>();

    // 对应你截图中的 PICO 骨骼命名风格 (下划线小写)
    private readonly string[] jointKeywords = new string[]
    {
        "wrist", "palm",
        "thumb_metacarpal", "thumb_proximal", "thumb_distal", "thumb_tip",
        "index_metacarpal", "index_proximal", "index_intermediate", "index_distal", "index_tip",
        "middle_metacarpal", "middle_proximal", "middle_intermediate", "middle_distal", "middle_tip",
        "ring_metacarpal", "ring_proximal", "ring_intermediate", "ring_distal", "ring_tip",
        "little_metacarpal", "little_proximal", "little_intermediate", "little_distal", "little_tip"
    };

    void Start()
    {
        // 游戏开始时，自动匹配左右手骨骼
        InitializeBones();
    }

    void InitializeBones()
    {
        bonePairs.Clear();
        foreach (string keyword in jointKeywords)
        {
            // 在左手中找包含 keyword 的骨骼 (例如 left_wrist)
            Transform r = FindDeep(realHandRoot, keyword);
            // 在右手中找包含 keyword 的骨骼 (例如 right_wrist)
            Transform m = FindDeep(mirrorHandRoot, keyword);

            if (r != null && m != null)
            {
                bonePairs.Add((r, m));
            }
        }
        Debug.Log($"镜像系统就绪：成功匹配 {bonePairs.Count} 对骨骼");
    }

    void Update()
    {
        // 1. 处理手腕根节点 (Root)
        // 位置镜像：X 轴取反
        Vector3 rootPos = realHandRoot.localPosition;
        rootPos.x = -rootPos.x;
        mirrorHandRoot.localPosition = rootPos;

        // 旋转镜像：Y 和 Z 轴取反
        Quaternion rootRot = realHandRoot.localRotation;
        rootRot.y = -rootRot.y;
        rootRot.z = -rootRot.z;
        mirrorHandRoot.localRotation = rootRot;

        // 2. 处理所有手指关节
        foreach (var pair in bonePairs)
        {
            // 位置镜像
            Vector3 targetPos = pair.real.localPosition;
            targetPos.x = -targetPos.x;

            // 旋转镜像
            Quaternion targetRot = pair.real.localRotation;
            targetRot.y = -targetRot.y;
            targetRot.z = -targetRot.z;

            // 平滑插值 (Lerp) 防止动作抖动
            pair.mirror.localPosition = Vector3.Lerp(pair.mirror.localPosition, targetPos, 1 - smoothFactor);
            pair.mirror.localRotation = Quaternion.Slerp(pair.mirror.localRotation, targetRot, 1 - smoothFactor);
        }
    }

    // 递归查找：只要名字里包含关键词就算找到
    Transform FindDeep(Transform parent, string keyword)
    {
        // 比如 keyword 是 "index_tip"，它能匹配 "right_index_tip"
        if (parent.name.Contains(keyword)) return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindDeep(child, keyword);
            if (result != null) return result;
        }
        return null;
    }
}