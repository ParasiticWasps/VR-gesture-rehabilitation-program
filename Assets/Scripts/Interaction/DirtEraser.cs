using UnityEngine;

public class DirtEraser : MonoBehaviour
{
    // 只要有东西进入这个触发器，就会调用这个函数
    void OnTriggerEnter(Collider other)
    {
        // 检查碰我的是不是刚才设置的 "index_tip" (食指尖)
        if (other.name.Contains("tip"))
        {
            // 如果是，就销毁我自己 (污渍消失)
            Destroy(gameObject);
        }
    }
}