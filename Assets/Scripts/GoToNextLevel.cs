using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToNextLevel : MonoBehaviour
{
    public void SceneTransition(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
