using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class SpawnObjectStruct
{
    [SerializeField] public string spawnPath;
    [SerializeField] public Vector3 position;
    [SerializeField] public Vector3 eulerAngles;
    [SerializeField] public Vector3 scale;
}

[Serializable] public class WipeObjectStruct 
{
    [SerializeField] public SpawnObjectStruct wipeSpawn;
    [SerializeField] public SpawnObjectStruct uiSpawn;
}

[CreateAssetMenu(fileName = "WipeSpawnObject", menuName = "ScriptableObjects/WipeSpawnObject", order = 1)]
public class WipeSpawnObject : ScriptableObject
{
    public List<WipeObjectStruct> wipesSpawn = new List<WipeObjectStruct>();
}
