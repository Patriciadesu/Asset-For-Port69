using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class JumpScareComtroller : PlayerExtension
{
    public Image Jumpscareimage;
    public AudioClip JumpscareClip;
    public AudioSource JumpScareSource;
    private bool hasBeenTriggered = false; 

    void Awake()
    {
        Jumpscareimage.enabled = false;
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Jumpscare"))
        {
            hasBeenTriggered = true;
            Jumpscareimage.enabled = true;
            JumpScareSource.PlayOneShot(JumpscareClip);
            StartCoroutine(CloseJumpscare());
            Destroy(other.gameObject); 

        }
    }
    private IEnumerator CloseJumpscare()
    {
        yield return new WaitForSeconds(2);
        Jumpscareimage.enabled = false;
    }

}
