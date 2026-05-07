using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class CollectZone : MonoBehaviour
{
    [Header("生成配置")]
    public GameObject prefab;           // 交互物体Prefab
    public Transform spawnCenter;       // 生成中心点
    public float spawnRadius = 1f;      // 随机范围半径
    public float spawnHeight = 1f;      // 生成高度
    public int maxActive = 5;           // 场景中最多活跃数量
    public float spawnInterval = 5f;    // 每隔几秒生成一个

    [Header("倒计时")]
    public float resetTime = 10f;

    [Header("UI")]
    public TextMeshProUGUI scoreText;

    private int score = 0;
    private List<GameObject> activeObjects = new();
    private Dictionary<GameObject, float> activeTimers = new();
    private float nextSpawnTime;

    private void Start()
    {
        // 开局生成4个
        for (int i = 0; i < 4; i++)
        {
            SpawnOne();
        }
        nextSpawnTime = Time.time + spawnInterval;
        UpdateScoreUI();
    }

    private void Update()
    {
        // 清理已销毁的引用
        activeObjects.RemoveAll(o => o == null || !o.activeSelf);

        // 定时生成，不超过最大数量
        if (Time.time >= nextSpawnTime)
        {
            if (activeObjects.Count < maxActive)
            {
                SpawnOne();
            }
            nextSpawnTime = Time.time + spawnInterval;
        }

        // 倒计时检测
        CheckTimers();
    }

    private void SpawnOne()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = spawnCenter.position + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);

        var obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        activeObjects.Add(obj);
        RegisterGrabEvents(obj);
    }

    private void RegisterGrabEvents(GameObject obj)
    {
        var grab = obj.GetComponent<XRGrabInteractable>();
        if (grab == null) return;

        grab.selectEntered.AddListener((args) =>
        {
            activeTimers[obj] = Time.time + resetTime;
        });
    }

    private void OnTriggerEnter(Collider other)
    {
        var grab = other.GetComponentInParent<XRGrabInteractable>();
        if (grab == null || !grab.enabled) return;

        // 取消倒计时
        activeTimers.Remove(grab.gameObject);

        score++;
        UpdateScoreUI();

        // 强制释放
        if (grab.isSelected)
        {
            grab.interactionManager.CancelInteractableSelection(
                (IXRSelectInteractable)grab);
        }

        // 禁用交互，保留刚体
        grab.enabled = false;

        // 失活
        grab.gameObject.SetActive(false);
    }

    private void CheckTimers()
    {
        if (activeTimers.Count == 0) return;

        List<GameObject> toReset = new();
        foreach (var kvp in activeTimers)
        {
            if (kvp.Key == null || !kvp.Key.activeSelf)
            {
                toReset.Add(kvp.Key);
                continue;
            }
            if (Time.time >= kvp.Value)
            {
                toReset.Add(kvp.Key);
            }
        }

        foreach (var obj in toReset)
        {
            activeTimers.Remove(obj);
            if (obj != null && obj.activeSelf)
            {
                // 超时：强制释放并失活
                var grab = obj.GetComponent<XRGrabInteractable>();
                if (grab != null && grab.isSelected)
                {
                    grab.interactionManager.CancelInteractableSelection(
                        (IXRSelectInteractable)grab);
                }
                obj.SetActive(false);
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnCenter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnCenter.position + Vector3.up * spawnHeight, spawnRadius);
        }
    }
}