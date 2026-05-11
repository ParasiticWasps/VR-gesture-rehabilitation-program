using UnityEngine;
using UnityEngine.UI;

public class WipeObjectSpawnManager : MonoBehaviour
{
    // —— 生成参数 ——
    [Header("生成需要的成员")]
    [SerializeField] private Transform  _wipeParent;
    [SerializeField] private Transform  _uiParent;

    // ——　ScriptsObject成员　——
    private const string WIPE_SPAWN_OBJECT_PATH = "ScriptableObjects/WipeSpawnObject";
    private WipeSpawnObject _wipeScriptableObject;

    // —— 其他组件 ——
    private GameToolkitManager _toolkit;

    private void Start()
    {
        Spawn();
    }

    public void Spawn()
    {
        if (TryGetGameToolkit())
        {
            _wipeScriptableObject = _toolkit.TryLoadObject<WipeSpawnObject>(WIPE_SPAWN_OBJECT_PATH);
            if (_wipeScriptableObject)
            {
                foreach (var element in _wipeScriptableObject.wipesSpawn)
                {
                    SpawnObjectStruct _wipeTrans = element.wipeSpawn, _uiTransform = element.uiSpawn;

                    GameObject  wipePrefab = _toolkit.TryLoadObject<GameObject>(element.wipeSpawn.spawnPath);
                    WipeSurface wipe       = _toolkit.InstantiateInTheScene<WipeSurface>(wipePrefab, _wipeParent, _wipeTrans.position, _wipeTrans.eulerAngles, _wipeTrans.scale);

                    GameObject uiPrefab = _toolkit.TryLoadObject<GameObject>(element.uiSpawn.spawnPath);
                    WipeSlider slider   = _toolkit.InstantiateInTheScene<WipeSlider>(uiPrefab, _uiParent, _uiTransform.position, _uiTransform.eulerAngles, _uiTransform.scale);

                    wipe.OnWipe += slider.OnChangedVlaue;
                }
            }
        }
    }

    // —— 工具方法 ——
    private bool TryGetGameToolkit()
    {
        GameToolkitManager obj = GameObject.FindAnyObjectByType<GameToolkitManager>();
        _toolkit = obj;
        return _toolkit != null;
    }
}
