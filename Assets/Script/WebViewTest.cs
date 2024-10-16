using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuplex.WebView;

//如果有要修改JavaScript的string的話
//請小心 " 和 ' ，要考慮到轉成javaScript後的格式

public class WebViewTest : MonoBehaviour
{
    public BaseWebViewPrefab webViewPrefab;
    bool isPauseVideo = false;
    async void Start()
    {
        await webViewPrefab.WaitUntilInitialized();
        Web.SetAutoplayEnabled(true);
        LoadHTML();
    }

    // Update is called once per frame
    void Update()
    {
        if(OVRInput.GetDown(OVRInput.RawButton.X))
        {
            isPauseVideo = !isPauseVideo;

            if (!isPauseVideo)
            {
                StartVideo();
            }
            else
            {
                StopVideo();
            }
        }

        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            PlayValidVideo();
        }
        else if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            PlayInvalidVideo();
        }
    }
    
    private async void PlayValidVideo()
    {
        Debug.Log("[WebViewTest]: Play valid video (Vid = yXwGF33g64Q)");
        await webViewPrefab.WebView.ExecuteJavaScript("player.loadVideoById('yXwGF33g64Q', 0);");
    }

    private async void PlayInvalidVideo()
    {
        Debug.Log("[WebViewTest]: Play invalid video (Vid = SGCMKTFBUUw)");
        await webViewPrefab.WebView.ExecuteJavaScript("player.loadVideoById('SGCMKTFBUUw', 0);");
    }

    private async void StartVideo()
    {
        Debug.Log("[WebViewTest]: Start video");
        await webViewPrefab.WebView.ExecuteJavaScript(@"player.playVideo();");
    }

    private async void StopVideo()
    {
        Debug.Log("[WebViewTest]: Pause video");
        await webViewPrefab.WebView.ExecuteJavaScript(@"player.pauseVideo()");
    }
    private async void ReceiveMassege(object sender, EventArgs<string> eventArgs)
    {
        Debug.Log($"[WebViewTest]: Receive Message: {eventArgs.Value}");

        // 做對應的動作
    }
    private void LoadHTML()
    {
        string htmlString = @"
        <!DOCTYPE html>
        <html>
            <body>
            <div id=""player""></div>
                <script>
                    // 2. This code loads the IFrame Player API code asynchronously.
                    var tag = document.createElement('script');

                    tag.src = ""https://www.youtube.com/iframe_api"";
                    var firstScriptTag = document.getElementsByTagName('script')[0];
                    firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);

                    // 3. This function creates an <iframe> (and YouTube player)
                    //    after the API code downloads.
                    var player;
                    function onYouTubeIframeAPIReady() {
                        player = new YT.Player('player', {
                            width: '720',
                            height: '480',
                            videoId: 'svZKD4267Ls',
                            playerVars: {
                            ""autoplay"": 1,
                            ""playsinline"": 1,
                            ""enablejsapi"": 1,
                            ""controls"": 0,
                            ""disablekb"": 1,
                            },
                            events: {
                                'onReady': onPlayerReady,
                                'onStateChange': onPlayerStateChange,
                                'onError': sendErrorMessage
                            }
                        });
                    }
                    
                    // API Events
                    function onPlayerReady(event) {  
                        window.vuplex.postMessage('Video is ready!');
                        window.vuplex.postMessage({ type: 'greeting', message: 'Try playVideo on ready'});
                        event.target.playVideo();
                    }
                    function sendErrorMessage(event){
                        window.vuplex.postMessage('Video has error!');
                        if(event.data == 101 || event.data == 150)
                        {
                            window.vuplex.postMessage({ message: 'Video not allowed embed'});
                        }
                        else if(event.data == 5)
                        {
                            window.vuplex.postMessage({ message: 'Video not Found'});
                        }
                    }
                    function onAutoPlayNotWorking()
                    {
                        player.playVideo();
                    }
                    function onPlayerStateChange(event) {
                        window.vuplex.postMessage('Video state changed!');
                        if (event.data == YT.PlayerState.BUFFERING) 
                        {
                            window.vuplex.postMessage({ message: 'Video State: Burrering' });
                        }
                        else if (event.data == YT.PlayerState.PLAYING) 
                        {
                            window.vuplex.postMessage({ message: 'Video State: Start' });
                        }
                        else if(event.data == YT.PlayerState.ENDED)
                        {
                            window.vuplex.postMessage({ message: 'Video State: End' });
                        }
                        else if(event.data == YT.PlayerState.PAUSED)
                        {
                            window.vuplex.postMessage({ message: 'Video State: Pause' });
                        }
                    }
                </script>
            </body>
        </html>";

        Debug.Log("[WebViewTest]: LoadHtml");
        webViewPrefab.WebView.LoadHtml(htmlString);
    }
}
