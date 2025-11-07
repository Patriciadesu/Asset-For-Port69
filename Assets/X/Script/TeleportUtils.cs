// ใส่ในสคริปต์ไหนก็ได้ที่เรียกเป็น Coroutine ได้ (เช่น PortalTeleport)
// call: StartCoroutine(PausePlayer(other.transform, 0.06f));

using System.Collections;
using UnityEngine;

public static class TeleportUtils
{
    public static IEnumerator PausePlayer(Transform player, float seconds = 0.05f)
    {
        var cc = player.GetComponent<CharacterController>();
        var rb = player.GetComponent<Rigidbody>();
        var anim = player.GetComponent<Animator>();

        // --- snapshot ---
        bool prevCCEnabled = cc ? cc.enabled : false;
        bool prevRoot = anim ? anim.applyRootMotion : false;
        bool prevDetect = false;
        RigidbodyInterpolation prevInterp = RigidbodyInterpolation.None;

        // --- hard stop motion ---
        if (cc) cc.enabled = false;

        if (rb)
        {
            prevDetect = rb.detectCollisions;
            prevInterp = rb.interpolation;

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity        = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;

            rb.detectCollisions = false;                 // เทียบเท่า “ปิด CC” ในคลิป
            rb.interpolation = RigidbodyInterpolation.None;
        }

        if (anim) anim.applyRootMotion = false;

        // --- wait physics-safe (เสถียรกว่า WaitForSeconds frame-based) ---
        float end = Time.time + seconds;
        while (Time.time < end) yield return new WaitForFixedUpdate();

        // --- rollback ---
        if (anim) anim.applyRootMotion = prevRoot;
        if (rb)
        {
            rb.detectCollisions = prevDetect;
            rb.interpolation = prevInterp;
            rb.WakeUp();
        }
        if (cc) cc.enabled = prevCCEnabled;
    }
}
