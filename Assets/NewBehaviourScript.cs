using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewBehaviourScript : MonoBehaviour
{
    public AudioClip clip;
    private void Start()
    {
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            AudioManager.Get().Play(clip);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
    }
}
