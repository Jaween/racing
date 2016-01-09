using UnityEngine;
using System.Collections;

public class MenuController : MonoBehaviour {
    
    public void LoadLevel(string sceneName)
    {
        Application.LoadLevel(sceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }
    
}
