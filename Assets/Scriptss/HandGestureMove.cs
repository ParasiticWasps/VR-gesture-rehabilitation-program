using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;


public class HandGestureMove : MonoBehaviour
{
    private static HandGestureMove _instance;

    public static HandGestureMove Get()
    {
        if (_instance == null)
            _instance = GameObject.FindAnyObjectByType<HandGestureMove>();
        return _instance;
    }

    [Header("移动")]
    public float moveSpeed = 1.5f;
    public bool isMoving = false;

    [Header("阈值")]
    [Range(0.03f, 0.07f)] public float curlThreshold = 0.06f;
    [Range(0.08f, 0.15f)] public float straightThreshold = 0.09f;
    [Range(0.06f, 0.14f)] public float thumbStraightThreshold = 0.08f;

    [Header("防抖")]
    public float holdTime = 0.15f;
    public int confirmFrames = 3;

    [Header("手部模型（拖入 XR Hand 可视化根节点）")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("镜像模式")]
    public bool enableMirror = true;

    // ===== 编辑器测试模式 =====
    [Header("== 编辑器测试（发布前关掉） ==")]
    [Tooltip("勾选后跳过 XRHandSubsystem，用下面的开关模拟追踪状态")]
    public bool editorTestMode = false;
    [Tooltip("模拟左手是否被追踪")]
    public bool testLeftTracked = false;
    [Tooltip("模拟右手是否被追踪")]
    public bool testRightTracked = false;
    [Tooltip("模拟手势：0=None, 1=ThumbUp, 2=OpenHand")]
    [Range(0, 2)] public int testGesture = 0;

    // ===== 内部状态 =====
    private Transform _cam;
    private XRHandSubsystem _handSubsystem;
    private CharacterController _cc;

    private enum Gesture { None, ThumbUp, OpenHand }
    private Gesture _confirmed = Gesture.None;
    private Gesture _pending   = Gesture.None;
    private int     _pendingCount;
    private float   _holdTimer;

    private bool _leftTracked;
    private bool _rightTracked;

    private Transform _mirrorOfLeft;
    private Transform _mirrorOfRight;

    // 需要在帧末隐藏渲染器的手（防止 XRI 在 LateUpdate 之后又打开）
    private bool _needHideLeft;
    private bool _needHideRight;
    private Coroutine _endOfFrameCoroutine;

    // ========================================================
    #region Lifecycle
    // ========================================================

    void Start()
    {
        _cam = Camera.main != null ? Camera.main.transform : null;
        _cc = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        _endOfFrameCoroutine = StartCoroutine(EndOfFrameLoop());
    }

    void OnDisable()
    {
        if (_endOfFrameCoroutine != null)
        {
            StopCoroutine(_endOfFrameCoroutine);
            _endOfFrameCoroutine = null;
        }
    }

    void Update()
    {
        if (_cam == null) return;

        if (editorTestMode)
        {
            _leftTracked  = testLeftTracked;
            _rightTracked = testRightTracked;
        }
        else
        {
            EnsureSubsystem();
            if (_handSubsystem == null || !_handSubsystem.running) return;
            _leftTracked  = IsHandTracked(Handedness.Left);
            _rightTracked = IsHandTracked(Handedness.Right);
        }

        if (enableMirror)
            TryCreateMirrors();

        // ---------- 手势检测 ----------
        Gesture left, right;
        if (editorTestMode)
        {
            Gesture g = (Gesture)testGesture;
            left  = _leftTracked  ? g : Gesture.None;
            right = _rightTracked ? g : Gesture.None;
        }
        else
        {
            left  = _leftTracked  ? DetectHand(Handedness.Left)  : Gesture.None;
            right = _rightTracked ? DetectHand(Handedness.Right) : Gesture.None;
        }

        if (enableMirror)
        {
            if (left  != Gesture.None && !_rightTracked) right = left;
            if (right != Gesture.None && !_leftTracked)  left  = right;
        }

        Gesture raw = Gesture.None;
        if      (left != Gesture.None && right == Gesture.None) raw = left;
        else if (right != Gesture.None && left == Gesture.None) raw = right;
        else if (left == right)                                  raw = left;

        if (raw == _pending)
            _pendingCount++;
        else
        {
            _pending      = raw;
            _pendingCount = 1;
        }
        if (_pendingCount >= confirmFrames)
            _confirmed = _pending;

        float dir = 0f;
        if (_confirmed == Gesture.ThumbUp)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer > holdTime) dir = 1f;
            isMoving = true;
        }
        else if (_confirmed == Gesture.OpenHand)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer > holdTime) dir = -1f;
            isMoving = true;
        }
        else
        {
            _holdTimer = 0f;
            isMoving = false;
        }

        if (dir == 0f) return;

        Vector3 fwd = _cam.forward;
        fwd.y = 0f;
        fwd.Normalize();

        // 用 CharacterController.Move 代替直接改 position，自动碰撞不穿墙
        if (_cc != null)
            _cc.Move(fwd * (dir * moveSpeed * Time.deltaTime));
        else
            transform.position += fwd * (dir * moveSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (!enableMirror) return;

        bool onlyLeft   = _leftTracked  && !_rightTracked;
        bool onlyRight  = !_leftTracked &&  _rightTracked;
        bool bothTracked = _leftTracked &&  _rightTracked;
        bool noneTracked = !_leftTracked && !_rightTracked;

        // 每帧重置标记
        _needHideLeft  = false;
        _needHideRight = false;

        if (bothTracked)
        {
            // 双手追踪到 → 显示真实手，关镜像
            ShowRenderers(leftHand);
            ShowRenderers(rightHand);
            SetMirrorActive(_mirrorOfLeft,  false);
            SetMirrorActive(_mirrorOfRight, false);
        }
        else if (noneTracked)
        {
            // 无手 → 全隐藏
            HideRenderers(leftHand);
            HideRenderers(rightHand);
            SetMirrorActive(_mirrorOfLeft,  false);
            SetMirrorActive(_mirrorOfRight, false);
            _needHideLeft  = true;
            _needHideRight = true;
        }
        else if (onlyLeft)
        {
            // 只有左手 → 隐藏真实手渲染器（脚本保留，骨骼继续更新）
            //           → 镜像手从真实手拷贝最新骨骼
            HideRenderers(leftHand);
            HideRenderers(rightHand);
            _needHideLeft  = true;
            _needHideRight = true;

            if (_mirrorOfLeft != null)
            {
                _mirrorOfLeft.gameObject.SetActive(true);
                DoMirror(leftHand, _mirrorOfLeft);
            }
            SetMirrorActive(_mirrorOfRight, false);
        }
        else if (onlyRight)
        {
            HideRenderers(leftHand);
            HideRenderers(rightHand);
            _needHideLeft  = true;
            _needHideRight = true;

            if (_mirrorOfRight != null)
            {
                _mirrorOfRight.gameObject.SetActive(true);
                DoMirror(rightHand, _mirrorOfRight);
            }
            SetMirrorActive(_mirrorOfLeft, false);
        }
    }

    /// <summary>
    /// 帧末协程：在渲染前最后一刻再关一次渲染器
    /// 防止 XRI 可视化组件在 LateUpdate 之后把渲染器重新打开
    /// </summary>
    IEnumerator EndOfFrameLoop()
    {
        var waitEOF = new WaitForEndOfFrame();
        while (true)
        {
            yield return waitEOF;
            if (_needHideLeft)  HideRenderers(leftHand);
            if (_needHideRight) HideRenderers(rightHand);
        }
    }

    void OnDestroy()
    {
        ShowRenderers(leftHand);
        ShowRenderers(rightHand);
        if (_mirrorOfLeft  != null) Destroy(_mirrorOfLeft.gameObject);
        if (_mirrorOfRight != null) Destroy(_mirrorOfRight.gameObject);
    }

    #endregion

    // ========================================================
    #region XR Hand Subsystem
    // ========================================================

    void EnsureSubsystem()
    {
        if (_handSubsystem != null && _handSubsystem.running) return;
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        _handSubsystem = subsystems.Count > 0 ? subsystems[0] : null;
    }

    XRHand GetXRHand(Handedness handedness)
    {
        return handedness == Handedness.Left
            ? _handSubsystem.leftHand
            : _handSubsystem.rightHand;
    }

    #endregion

    // ========================================================
    #region 追踪 & 手势检测
    // ========================================================

    bool IsHandTracked(Handedness handedness)
    {
        if (_handSubsystem == null) return false;
        return GetXRHand(handedness).isTracked;
    }

    Gesture DetectHand(Handedness handedness)
    {
        XRHand hand = GetXRHand(handedness);
        if (!hand.isTracked) return Gesture.None;

        if (!TryGetJointPosition(hand, XRHandJointID.Palm,      out Vector3 palm))      return Gesture.None;
        if (!TryGetJointPosition(hand, XRHandJointID.ThumbTip,  out Vector3 thumbTip))  return Gesture.None;
        if (!TryGetJointPosition(hand, XRHandJointID.IndexTip,  out Vector3 indexTip))  return Gesture.None;
        if (!TryGetJointPosition(hand, XRHandJointID.MiddleTip, out Vector3 middleTip)) return Gesture.None;
        if (!TryGetJointPosition(hand, XRHandJointID.RingTip,   out Vector3 ringTip))   return Gesture.None;
        if (!TryGetJointPosition(hand, XRHandJointID.LittleTip, out Vector3 littleTip)) return Gesture.None;

        return AnalyzeGesture(palm, thumbTip, indexTip, middleTip, ringTip, littleTip);
    }

    bool TryGetJointPosition(XRHand hand, XRHandJointID jointID, out Vector3 position)
    {
        XRHandJoint joint = hand.GetJoint(jointID);
        if (joint.TryGetPose(out Pose pose))
        {
            position = pose.position;
            return true;
        }
        position = Vector3.zero;
        return false;
    }

    Gesture AnalyzeGesture(Vector3 palm, Vector3 thumbTip, Vector3 indexTip,
                           Vector3 middleTip, Vector3 ringTip, Vector3 littleTip)
    {
        float thumbDist  = Vector3.Distance(thumbTip,  palm);
        float indexDist  = Vector3.Distance(indexTip,  palm);
        float middleDist = Vector3.Distance(middleTip, palm);
        float ringDist   = Vector3.Distance(ringTip,   palm);
        float littleDist = Vector3.Distance(littleTip, palm);

        bool thumbOut     = thumbDist > thumbStraightThreshold;
        bool fourCurled   = indexDist  < curlThreshold
                         && middleDist < curlThreshold
                         && ringDist   < curlThreshold
                         && littleDist < curlThreshold;
        bool fourStraight = indexDist  > straightThreshold
                         && middleDist > straightThreshold
                         && ringDist   > straightThreshold
                         && littleDist > straightThreshold;

        if (thumbOut && fourCurled)   return Gesture.ThumbUp;
        if (thumbOut && fourStraight) return Gesture.OpenHand;
        return Gesture.None;
    }

    #endregion

    // ========================================================
    #region 手部显隐（只控制渲染器，脚本全部保留，骨骼持续更新）
    // ========================================================

    void HideRenderers(Transform hand)
    {
        if (hand == null) return;
        foreach (var r in hand.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
    }

    void ShowRenderers(Transform hand)
    {
        if (hand == null) return;
        foreach (var r in hand.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
    }

    static void SetMirrorActive(Transform mirror, bool active)
    {
        if (mirror != null) mirror.gameObject.SetActive(active);
    }

    #endregion

    // ========================================================
    #region 镜像系统（镜像副本删除脚本，只保留网格，靠 SyncBones 驱动）
    // ========================================================

    void TryCreateMirrors()
    {
        if (_mirrorOfLeft == null && leftHand != null)
        {
            bool wasActive = leftHand.gameObject.activeSelf;
            if (!wasActive) leftHand.gameObject.SetActive(true);
            _mirrorOfLeft = CreateMirrorCopy(leftHand, "MirrorOfLeft");
            if (!wasActive) leftHand.gameObject.SetActive(false);
        }

        if (_mirrorOfRight == null && rightHand != null)
        {
            bool wasActive = rightHand.gameObject.activeSelf;
            if (!wasActive) rightHand.gameObject.SetActive(true);
            _mirrorOfRight = CreateMirrorCopy(rightHand, "MirrorOfRight");
            if (!wasActive) rightHand.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 创建镜像副本：删除脚本和动画器（纯渲染空壳），骨骼由 SyncBones 每帧驱动
    /// </summary>
    Transform CreateMirrorCopy(Transform source, string name)
    {
        GameObject copy = Instantiate(source.gameObject, source.parent);
        copy.name = name;

        // 镜像副本必须删脚本，否则 XRI 会用自己的数据驱动骨骼，而不是走镜像逻辑
        foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>(true))
            Destroy(mb);
        foreach (var anim in copy.GetComponentsInChildren<Animator>(true))
            Destroy(anim);

        foreach (var r in copy.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;

        copy.SetActive(false);
        return copy.transform;
    }

    /// <summary>
    /// 把真实手的位置/旋转做 X 轴镜像，再同步所有骨骼
    /// </summary>
    void DoMirror(Transform source, Transform mirror)
    {
        // 位置镜像
        Vector3 localPos = _cam.InverseTransformPoint(source.position);
        localPos.x = -localPos.x;
        mirror.position = _cam.TransformPoint(localPos);

        // 旋转镜像
        Quaternion localRot = Quaternion.Inverse(_cam.rotation) * source.rotation;
        localRot = new Quaternion(-localRot.x, localRot.y, localRot.z, -localRot.w);
        mirror.rotation = _cam.rotation * localRot;

        // 缩放镜像
        Vector3 s = source.localScale;
        s.x = -s.x;
        mirror.localScale = s;

        // 逐骨骼同步（真实手骨骼由 XRI 实时更新，这里拷贝最新数据）
        SyncBones(source, mirror);
    }

    void SyncBones(Transform src, Transform dst)
    {
        for (int i = 0; i < src.childCount && i < dst.childCount; i++)
        {
            Transform srcChild = src.GetChild(i);
            Transform dstChild = dst.GetChild(i);
            dstChild.localPosition = srcChild.localPosition;
            dstChild.localRotation = srcChild.localRotation;
            if (srcChild.childCount > 0)
                SyncBones(srcChild, dstChild);
        }
    }

    #endregion
}
