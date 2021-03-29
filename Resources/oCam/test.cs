using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class test : MonoBehaviour
{
    [DllImport("oCam")]
    private static extern bool Connect(int width, int height, double fps);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
