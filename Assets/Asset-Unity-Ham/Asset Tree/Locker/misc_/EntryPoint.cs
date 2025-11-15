using UnityEngine;

public class EntryPoint : MonoBehaviour
{
   public bool isInzone = false;
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInzone = true;
            Debug.Log("Player is in the zone");
            Debug.Log(isInzone);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInzone = false;
        }
    }

}
