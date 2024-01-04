using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DontDestroyScripts : MonoBehaviour
{
    // The static instance variable that holds the single instance of the GameManager
    private static DontDestroyScripts _instance;

    // Public property to access the GameManager instance
    public static DontDestroyScripts Instance
    {
        get
        {
            // If there is no instance yet, find one or create a new one
            if (_instance == null)
            {
                _instance = FindObjectOfType<DontDestroyScripts>();

                // If no instance was found in the scene, create a new GameObject and attach the GameManager script
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("DontDestroyScripts");
                    _instance = singletonObject.AddComponent<DontDestroyScripts>();
                }
            }

            return _instance;
        }
    }

    // Any other variables and methods you want for your GameManager can go below

    private void Awake()
    {
        // Ensure that there is only one instance of the GameManager
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject); // This ensures that the GameManager persists between scenes
        }
    }
}
