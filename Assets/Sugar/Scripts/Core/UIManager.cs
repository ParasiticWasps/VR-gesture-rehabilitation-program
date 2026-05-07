using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;

    public static UIManager Get()
    {
        if (_instance == null)
            _instance = GameObject.FindAnyObjectByType<UIManager>();
        return _instance;
    }

    [SerializeField] private TextMeshProUGUI _forwardDistanceText;
    [SerializeField] private TextMeshProUGUI _yawDistanceText;
    [SerializeField] private TextMeshProUGUI _shrugDistanceText;
    [SerializeField] private Text _warningText;

    public void SetText(TextMeshProUGUI t, string mess)
    {
        t.text = mess;
    }

    public void SetText(Text t, string mess)
    {
        t.text = mess;
    }

    // —— 具体功能 ——
    public void SetForwardDistanceText(string mess)
    {
        SetText(_forwardDistanceText, mess);
    }

    public void SetYawDistanceText(string mess)
    {
        SetText(_yawDistanceText, mess);
    }

    public void SetShrugDistanceText(string mess)
    {
        SetText(_shrugDistanceText, mess);
    }

    public void SetWarningText(string warning)
    {
        SetText(_warningText, warning);
    }
}
