using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ClothWiper : MonoBehaviour
{
    [Header("调试")]
    [Tooltip("勾选后不需要抓取也能擦，方便编辑器测试")]
    public bool debugMode = false;

    public bool IsGrabbed => debugMode || _isGrabbed;

    private bool _isGrabbed = false;
    private XRGrabInteractable grab;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (grab == null) return;
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        if (grab == null) return;
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args) => _isGrabbed = true;
    private void OnRelease(SelectExitEventArgs args) => _isGrabbed = false;
}