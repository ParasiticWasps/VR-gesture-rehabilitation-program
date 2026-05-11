using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartCollider : MonoBehaviour
{
    public GameObject gui_1;
    public GameObject gui_2;

    private void Awake()
    {
        gui_1.gameObject.SetActive(true);
        gui_2.gameObject.SetActive(false);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 进入触发器时执行的逻辑
            Debug.Log("Player entered the trigger!");
            // 可以在这里添加其他逻辑，例如播放动画、触发事件等

            gui_1.gameObject.SetActive(false);
            gui_2.gameObject.SetActive(true);

            StartCoroutine(CloaseAll());
        }
    }

    private IEnumerator CloaseAll()
    {
        yield return new WaitForSeconds(3f);
        gui_2.gameObject.SetActive(false);
    }
}
