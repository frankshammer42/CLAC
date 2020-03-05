using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoviePlayer : MonoBehaviour
{
    public GameObject camera;
    
    // Start is called before the first frame update
    void Start()
    {
        // Will attach a VideoPlayer to the main camera.

        // VideoPlayer automatically targets the camera backplane when it is added
        // to a camera object, no need to change videoPlayer.targetCamera.
        var videoPlayer = camera.AddComponent<UnityEngine.Video.VideoPlayer>();

        // below to auto-start playback since we're in Start().
        videoPlayer.playOnAwake = false;

        // By default, VideoPlayers added to a camera will use the far plane.
        // Let's target the near plane instead.
        videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.CameraFarPlane;

        // This will cause our Scene to be visible through the video being played.
        videoPlayer.targetCameraAlpha = 0.5F;
        videoPlayer.url = "Assets/Tokyo.mp4";
        // Skip the first 100 frames.
        // Restart from beginning when done.
        videoPlayer.frame = 600;
        videoPlayer.isLooping = true;
        videoPlayer.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
