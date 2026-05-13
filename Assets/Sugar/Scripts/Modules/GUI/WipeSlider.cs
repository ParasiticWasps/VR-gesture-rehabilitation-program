using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WipeSlider : MonoBehaviour
{
    private Slider _slider;

    private void Start()
    {
        _slider = GetComponent<Slider>();
        if (!_slider) _slider = gameObject.AddComponent<Slider>();
    }

    public void OnChangedVlaue(float val)
    {
        _slider.value = 1.0f - val;
    }
}
