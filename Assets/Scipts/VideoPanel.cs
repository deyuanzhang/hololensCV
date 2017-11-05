using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoPanel : MonoBehaviour {

    public RawImage rawImage;

    //initialize the resolution of the texture to be displayed on the canvas
    public void ResolutionInit(int width, int height)
    {
        var newTexture = new Texture2D(width, height, TextureFormat.BGRA32, false);
        rawImage.texture = newTexture;
    }

    //change the image currently displayed on the canvas
    public void SetImage(byte[] _latestImageBytes)
    {
        var currentTexture = rawImage.texture as Texture2D;
        currentTexture.LoadRawTextureData(_latestImageBytes);
        currentTexture.Apply();
    }
}
