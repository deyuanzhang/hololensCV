//  
// Copyright (c) 2017 Vulcan, Inc. All rights reserved.  
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.
//

using UnityEngine;
using UnityEngine.VR.WSA;
using System;
using HoloLensCameraStream;
using System.IO;
using System.Threading;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This example gets the video frames at 30 fps and displays them on a Unity texture,
/// which is locked the User's gaze.
/// </summary>
namespace CleanRebuild
{
    public class VideoPanelHandler : MonoBehaviour
    {
        byte[] _latestImageBytes;
        HoloLensCameraStream.Resolution _resolution;

        NetworkHandler networkController;

        //"Injected" objects.
        VideoPanel _videoPanelUI;
        VideoCapture _videoCapture;

        IntPtr _spatialCoordinateSystemPtr;

        void Start()
        {
            //Fetch a pointer to Unity's spatial coordinate system if you need pixel mapping
            _spatialCoordinateSystemPtr = WorldManager.GetNativeISpatialCoordinateSystemPtr();

            //Call this in Start() to ensure that the CameraStreamHelper is already "Awake".
            CameraStreamHelper.Instance.GetVideoCaptureAsync(OnVideoCaptureCreated);
            //You could also do this "shortcut":
            //CameraStreamManager.Instance.GetVideoCaptureAsync(v => videoCapture = v);

            _videoPanelUI = GameObject.FindObjectOfType<VideoPanel>();
            networkController = GameObject.FindObjectOfType<NetworkHandler>();
        }

        //This causes some weird frame sync timig issues, consider calling on the app thread (that crashes debug)
        void Update()
        {
            if (_latestImageBytes != null)
            {
                _videoPanelUI.SetImage(_latestImageBytes);
            }
        }

        private void OnDestroy()
        {
            if (_videoCapture != null)
            {
                _videoCapture.FrameSampleAcquired -= OnFrameSampleAcquired;
                _videoCapture.Dispose();
            }
        }

        void OnVideoCaptureCreated(VideoCapture videoCapture)
        {
            if (videoCapture == null)
            {
                Debug.LogError("Did not find a video capture object. You may not be using the HoloLens.");
                return;
            }

            this._videoCapture = videoCapture;

            //Request the spatial coordinate ptr if you want fetch the camera and set it if you need to 
            CameraStreamHelper.Instance.SetNativeISpatialCoordinateSystemPtr(_spatialCoordinateSystemPtr);

            _resolution = CameraStreamHelper.Instance.GetLowestResolution();
            float frameRate = CameraStreamHelper.Instance.GetHighestFrameRate(_resolution);
            videoCapture.FrameSampleAcquired += OnFrameSampleAcquired;

            //You don't need to set all of these params.
            //I'm just adding them to show you that they exist.
            CameraParameters cameraParams = new CameraParameters();
            cameraParams.cameraResolutionHeight = _resolution.height;
            cameraParams.cameraResolutionWidth = _resolution.width;
            cameraParams.frameRate = Mathf.RoundToInt(frameRate);
            cameraParams.pixelFormat = CapturePixelFormat.BGRA32;
            cameraParams.rotateImage180Degrees = true; //If your image is upside down, remove this line.
            cameraParams.enableHolograms = false;

            _videoPanelUI.ResolutionInit(_resolution.width, _resolution.height);

            videoCapture.StartVideoModeAsync(cameraParams, OnVideoModeStarted);

        }

        void OnVideoModeStarted(VideoCaptureResult result)
        {
            if (result.success == false)
            {
                Debug.LogWarning("Could not start video mode.");
                return;
            }

            Debug.Log("Video capture started.");
        }

        void OnFrameSampleAcquired(VideoCaptureSample sample)
        {
            //When copying the bytes out of the buffer, you must supply a byte[] that is appropriately sized.
            //You can reuse this byte[] until you need to resize it (for whatever reason).
            if (_latestImageBytes == null || _latestImageBytes.Length < sample.dataLength)
            {
                _latestImageBytes = new byte[sample.dataLength];
            }
            sample.CopyRawImageDataIntoBuffer(_latestImageBytes);

            //If you need to get the cameraToWorld matrix for purposes of compositing you can do it like this
            float[] cameraToWorldMatrix;
            if (sample.TryGetCameraToWorldMatrix(out cameraToWorldMatrix) == false)
            {
                return;
            }

            //If you need to get the projection matrix for purposes of compositing you can do it like this
            float[] projectionMatrix;
            if (sample.TryGetProjectionMatrix(out projectionMatrix) == false)
            {
                return;
            }

            //Start sending the image data over the network
            networkController.StartExchange(_latestImageBytes);

            sample.Dispose();

        }

        //Retrieve the current byte array for access
        public byte[] RequestImageBytes()
        {
            return _latestImageBytes;
        }

        //Convert the byte array into a sparse matrix
        private byte[] ConvertToSparse()
        {

            //Hold the pixels that pass the thresholding limit
            List<byte> acceptedPixels = new List<byte>();
            //Count the number of white pixels in between that didn't pass the threshold
            int count = 0;

            //We are receiving the bytes in BGRA order
            int stride = 4;
            float denominator = 1.0f / 255.0f;
            List<Color> colorArray = new List<Color>();
            for (int i = 0; i <= _latestImageBytes.Length - 1; i += stride)
            {
                float a = (int)(_latestImageBytes[i + 3]) * denominator;
                float r = (int)(_latestImageBytes[i + 2]) * denominator;
                float g = (int)(_latestImageBytes[i + 1]) * denominator;
                float b = (int)(_latestImageBytes[i]) * denominator;

                //Attempt image thresholding, remove all 
                double threshold = 1.3;
                Boolean notWhite = (r * threshold < 255 && b * threshold < 255 && g * threshold < 255);
                if (notWhite)
                {
                    acceptedPixels.Add((byte)count);
                    acceptedPixels.Add((byte)b);
                    acceptedPixels.Add((byte)g);
                    acceptedPixels.Add((byte)r);
                    count = 0;
                }
                else
                {
                    count++;
                }
            }

            byte[] compressedImageArray = new byte[acceptedPixels.Count];
            for (int i = 0; i < acceptedPixels.Count; i++)
            {
                compressedImageArray[i] = acceptedPixels[i];
            }

            return compressedImageArray;
        }
    }
}