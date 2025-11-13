using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Destroyrange : MonoBehaviour
{
    public float range = 5f; 
    public string playerTag = "Player";
    private Transform player;
    private float holdTime = 0f;
    private float requiredHoldTime = 5f; 
    private bool isInRange = false;
    private bool isHidingUI = false; 

    public Image Holdbar;
    public GameObject UI_POP_UP;
    public Animator animator;

    void Update()
    {

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
                player = p.transform;
        }

        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            bool wasInRange = isInRange;
            isInRange = distance <= range;

            if (isInRange)
            {
          
                if (!wasInRange)
                {
                    if (isHidingUI) StopAllCoroutines();
                    UI_POP_UP.SetActive(true);
                    animator.Play("GETTING NEAR");
                }

                if (Input.GetKey(KeyCode.E))
                {
                    holdTime += Time.deltaTime;
                    Holdbar.fillAmount = holdTime / requiredHoldTime;

                    if (holdTime >= requiredHoldTime)
                    {
                        Destroy(gameObject);
                    }
                }
                else
                {
                    holdTime = 0f;
                    Holdbar.fillAmount = 0f;
                }
            }
            else
            {
                // เริ่มดีเลย์ก่อนปิด UI
                if (wasInRange && !isHidingUI)
                {
                    StartCoroutine(HideUIAfterDelay(0.5f));
                    animator.Play("LEAVING OUT");
                }

                holdTime = 0f;
                Holdbar.fillAmount = 0f;
            }
        }
    }

    IEnumerator HideUIAfterDelay(float delay)
    {
        isHidingUI = true;
        yield return new WaitForSeconds(delay);
        UI_POP_UP.SetActive(false);
        isHidingUI = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
