using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO;
#if UNITY_UWP
using System.Threading.Tasks;
#endif

namespace CleanRebuild
{
    public class NetworkHandler : MonoBehaviour
    {

        VideoPanelHandler _videoHandler;
#if UNITY_UWP
    private Windows.Networking.Sockets.StreamSocket socket;
    private Task dataExchange;
    private StreamWriter writer;
    private StreamReader reader;
    private bool stopExchange;
    private bool exchanging;
#endif

        // Use this for initialization
        void Start()
        {
            _videoHandler = GameObject.FindObjectOfType<VideoPanelHandler>();
#if UNITY_UWP
        ConnectToRemote();
#endif
        }

        // Update is called once per frame
        void Update()
        {

        }

        //Create a socket and attempt connection to remote server
        //In future, an entry from a 2D UWP application can provide inputs into the hard coded values
#if UNITY_UWP
    async void ConnectToRemote()
        {
            try 
            {
                //if  (dataExchange != null)
                  //  StopExchange();
                //Socket setup
                socket = new Windows.Networking.Sockets.StreamSocket();
                Windows.Networking.HostName serverHost = new Windows.Networking.HostName("40.76.194.87");
                await socket.ConnectAsync(serverHost, "62520");

                Debug.LogError("connection successful");
                Stream outStream = socket.OutputStream.AsStreamForWrite();
                writer = new StreamWriter(outStream);
                Stream inStream = socket.InputStream.AsStreamForRead();
                reader = new StreamReader(inStream);

            }
            catch (System.Exception except)
            {
                Debug.LogError(except.ToString());
            }
            

        }
#endif

        //Initialize the data exchange task
#if UNITY_UWP
    public void StartExchange(byte[] arr)
    {
        //if (dataExchange != null)
          //  StopExchange();
        dataExchange = Task.Run(() => ExchangeData(arr));
        stopExchange = false;
    }
#endif

#if !UNITY_UWP
        public void StartExchange(byte[] latestImageBytes)
        {
            throw new NotImplementedException();
        }
#endif

        //Run the data transmission within it's own seperate thread. Unity does not support
        //threads by default so the method must be implemented within a task
#if UNITY_UWP
    void ExchangeData(byte[] arr)
    {
        exchanging = true;
        Debug.LogError("Working");
        writer.Write("Starting stream");
        foreach (byte b in arr){
            writer.Write("1");
        }
        writer.Write("End of stream");
        Debug.LogError("Data Sent");
    }
#endif

        //Call to free resources and reinitialize the application variables
/*
#if UNITY_UWP
    void StopExchange()
    {
        stopExchange = true;
        exchanging = false;
        dataExchange.Wait();
        socket.Dispose();
        writer.Dispose();
        reader.Dispose();
        dataExchange = null;
        socket = null;
        writer = null;
        reader = null;
    }

#endif
    */
    }


}