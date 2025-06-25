using UnityEngine;

public class SimpleWebcamTest : MonoBehaviour
{
    private WebCamTexture webcamTexture;

    void Start()
    {
        webcamTexture = new WebCamTexture();
        Renderer rend = GetComponent<Renderer>();
        rend.material.mainTexture = webcamTexture;
        webcamTexture.Play();
    }

    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
            webcamTexture.Stop();
    }
}
