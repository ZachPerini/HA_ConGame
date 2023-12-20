using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontDestroyScripts : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}
