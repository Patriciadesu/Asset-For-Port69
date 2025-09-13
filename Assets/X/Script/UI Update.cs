using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUpdate : MonoBehaviour
{
    [Header("References")]
    public Player _player;     
    public Image staminaFill;   

    void Update()
    {
        staminaFill.fillAmount = _player.Stat.currentstamina / _player.Stat.maxstamina;
    }
}
