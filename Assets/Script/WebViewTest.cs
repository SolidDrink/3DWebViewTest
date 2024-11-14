using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Vuplex.WebView;

//如果有要修改JavaScript的string的話
//請小心 " 和 ' ，要考慮到轉成javaScript後的格式

public class WebViewTest : MonoBehaviour
{
    public RectTransform canvasRectTransform;
    public BaseWebViewPrefab webViewPrefab;
    bool isPauseVideo = false;
    bool isFirstPause = true;
    async void Start()
    {
        await webViewPrefab.WaitUntilInitialized();
        webViewPrefab.WebView.MessageEmitted += ReceiveMassege;
        Web.SetAutoplayEnabled(true);
        LoadHTML();
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.X))
        {
            isPauseVideo = !isPauseVideo;

            if (!isPauseVideo)
            {
                StartVideo();
            }
            else
            {
                PauseVideo();
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

    private async void SimulateClicking()
    {
        Debug.Log("[WebViewTest]: Click Canvas");
        Vector2 canvasCenter = new Vector2(canvasRectTransform.rect.width / 2, canvasRectTransform.rect.height / 2);
        webViewPrefab.WebView.Click(canvasCenter.normalized);
    }

    #region Youtube Operate
    private async void Unmute()
    {
        Debug.Log("[WebViewTest]: unMute");
        await webViewPrefab.WebView.ExecuteJavaScript("player.unMute();");
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

    private async void PauseVideo()
    {
        Debug.Log("[WebViewTest]: Pause video");
        await webViewPrefab.WebView.ExecuteJavaScript(@"player.pauseVideo()");
    }
    #endregion

    private async void ReceiveMassege(object sender, EventArgs<string> eventArgs)
    {
        Debug.Log($"[WebView]: Receive Message: {eventArgs.Value}");

        if (eventArgs.Value.Contains("Ready"))
        {
            SimulateClicking();
            SimulateClicking();
            
        }
        else if (eventArgs.Value.Contains("stateType"))
        {
            YoutubeState state = JsonUtility.FromJson<YoutubeState>(eventArgs.Value);

            OnStateChange(state);
        }
        else if (eventArgs.Value.Contains("errorType"))
        {
            YoutubeError error = JsonUtility.FromJson<YoutubeError>(eventArgs.Value);

            OnErrorOccurs(error);
        }
        else
        {
            YoutubeVideoTime youtubeTimeInfo = JsonUtility.FromJson<YoutubeVideoTime>(eventArgs.Value);
            //Debug.Log($"videoTime = {youtubeTimeInfo.currentTime}");
        }
    }

    private void OnStateChange(YoutubeState newState)
    {
        switch (newState.stateType)
        {
            case YoutubeState.YoutubeStateType.PLAYING:
                if (isFirstPause)
                {
                    Unmute();
                }
                break;

            case YoutubeState.YoutubeStateType.ENDED:

                break;

            case YoutubeState.YoutubeStateType.PAUSED:

                if (isFirstPause)
                {
                    SimulateClicking();
                    
                    isFirstPause = false;
                    webViewPrefab.WebView.ExecuteJavaScript(@"disablePointerInteract();");
                }
                break;
        }
    }
    private void OnErrorOccurs(YoutubeError error)
    {
        switch (error.errorType)
        {
            case YoutubeError.YoutubeErrorType.INVALID_ID:

                break;

            case YoutubeError.YoutubeErrorType.VIDEO_NOT_FOUND:

                break;


            case YoutubeError.YoutubeErrorType.EMBEDDED_NOT_ALLOWED:

                break;

            case YoutubeError.YoutubeErrorType.EMBEDDED_ERROR:

                break;
        }
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
                    var isFirstPause = true;
                    tag.src = ""https://www.youtube.com/iframe_api"";
                    var firstScriptTag = document.getElementsByTagName('script')[0];
                    firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);

                    // 3. This function creates an <iframe> (and YouTube player)
                    //    after the API code downloads.
                    var player;
                    function onYouTubeIframeAPIReady() {
                        player = new YT.Player('player', {
                            width: '1280',
                            height: '560',
                            videoId: 'SGCMKTFBUUw',
                            playerVars: {
                            ""mute"": 1,
                            ""autoplay"": 1,
                            ""playsinline"": 1,
                            ""enablejsapi"": 1,
                            ""controls"": 0,
                            ""disablekb"": 1,
                            ""origin"": ""https://www.global-media.com.tw""
                            },
                            events: {
                                'onReady': onPlayerReady,
                                'onStateChange': onPlayerStateChange,
                                'onError': sendErrorMessage
                            }
                        });
                    }
                    
                    function getCurrentTime(){
                        var state = player.getPlayerState();
                        if(state != YT.PlayerState.PLAYING)
                            return;
                        
                        var time = player.getCurrentTime();
                        window.vuplex.postMessage({ currentTime: time});
                    }
                    
                    function disablePointerInteract() {
                        const style = document.createElement('style');

                        // 2. 添加 CSS 規則到 <style> 標籤
                        style.textContent = `
                          #player {pointer-events: none}
                        `;

                        // 3. 將 <style> 標籤加入到 <head> 中
                        document.head.appendChild(style);
                    }
                    
                    // API Events
                    function onPlayerReady(event) {  
                         window.vuplex.postMessage({ message: 'Video State: Ready' });

                         window.setInterval(getCurrentTime, 500);
                    }
                    function sendErrorMessage(event){
                        window.vuplex.postMessage({ errorType: event.data });
                    }
                    function onAutoPlayNotWorking()
                    {
                        player.playVideo();
                    }
                    function onPlayerStateChange(event) {
                        window.vuplex.postMessage({ stateType: event.data } );
                    }
                    
                </script>
            </body>
        </html>";

#if UNITY_ANDROID && !UNITY_EDITOR
        var androidWebView = webViewPrefab.WebView as AndroidWebView;
        androidWebView.LoadHtml(htmlString, "https://www.global-media.com.tw");
#endif
    }
}

public class YoutubeState
{
    public YoutubeStateType stateType;
    public enum YoutubeStateType
    {
        UNSTARTED = -1,
        ENDED = 0,
        PLAYING = 1,
        PAUSED = 2,
        BUFFERING = 3,
        CUED = 5,
    }
}

public class YoutubeError
{
    public YoutubeErrorType errorType;
    public enum YoutubeErrorType
    {
        INVALID_ID = 2,
        HTML5_ERROR = 5,
        VIDEO_NOT_FOUND = 100,
        EMBEDDED_NOT_ALLOWED = 101,
        EMBEDDED_ERROR = 105,
    }
}

public class YoutubeVideoTime
{
    public float currentTime;

}