using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Video;

public class VideoTrigger : ObjectEffect
{
    public VideoClip videoClip;
    private VideoPlayer videoPlayer;
    public bool playOnlyOnce;
    public void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = this.AddComponent<VideoPlayer>();
        }
    }
    public override void ApplyEffect(Player player)
    {
        base.ApplyEffect(player);
        videoPlayer.clip = videoClip;
        videoPlayer.Play();
        Destroy(this.gameObject, videoPlayer.clip.length.ConvertTo<float>()+0.2f);
    }

}