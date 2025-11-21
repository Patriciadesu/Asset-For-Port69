using UnityEngine;
using NaughtyAttributes;
using UnityEngine.SceneManagement;
public class SceneChanger : ObjectEffect
{
    [Scene] public string scene;
    public override void ApplyEffect(GameObject player)
    {
        base.ApplyEffect(player);
        ChangeScene(scene);
    }
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
