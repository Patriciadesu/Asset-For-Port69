using UnityEngine;

public class Player : Singleton<Player>
{
    public Vector3 position;
    public int money;
    public int health;
    public Inventory inventory;
}

public class SceneChanger : MonoBehaviour
{
    public void ChangeScene(string sceneName)
    {
        // Save current player data before changing scenes
        PlayerData.Instance.position = Player.Instance.position;
        PlayerData.Instance.lastScene = sceneName;
        PlayerData.Instance.isOnBoat = false; // Example, set based on game logic
        PlayerData.Instance.money = Player.Instance.money;
        PlayerData.Instance.health = Player.Instance.health;

        // Load the new scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        SetPlayerData();
    }


    public void SetPlayerData()
    {
        Player.Instance.position = PlayerData.Instance.position;
        Player.Instance.money = PlayerData.Instance.money;
        Player.Instance.health = PlayerData.Instance.health;
        Player.Instance.inventory = PlayerData.Instance.inventory;
    }
    public void SavePlayerData()
    {
        PlayerData.Instance.position = Player.Instance.position;
        PlayerData.Instance.money = Player.Instance.money;
        PlayerData.Instance.health = Player.Instance.health;
        PlayerData.Instance.inventory = Player.Instance.inventory;
    }
}
