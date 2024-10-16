// Copyright (c) 2024 Vuplex Inc. All rights reserved.
//
// Licensed under the Vuplex Commercial Software Library License, you may
// not use this file except in compliance with the License. You may obtain
// a copy of the License at
//
//     https://vuplex.com/commercial-library-license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#if UNITY_ANDROID && !UNITY_EDITOR
#pragma warning disable CS0067
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView {

    /// <summary>
    /// AndroidGeckoWebView is the IWebView implementation used by 3D WebView for Android with Gecko Engine.
    /// It also includes additional APIs for Gecko-specific functionality.
    /// </summary>
    /// <remarks>
    /// The Android Gecko plugin supports dragging within a web page to select
    /// text but doesn't support drag-and-drop interactions.
    /// </remarks>
    public class AndroidGeckoWebView : BaseWebView,
                                       IWebView,
                                       IWithAuth,
                                       IWithChangingTexture,
                                       IWithDownloads,
                                       IWithFileSelection,
                                       IWithFind,
                                       IWithKeyDownAndUp,
                                       IWithMovablePointer,
                                       IWithMutableAudio,
                                       IWithNativeJavaScriptDialogs,
                                       IWithPointerDownAndUp,
                                       IWithPopups,
                                       IWithSettableUserAgent,
                                       IWithTouch {

        /// <see cref="IWithAuth"/>
        public event EventHandler<AuthRequestedEventArgs> AuthRequested {
            add {
                _assertSingletonEventHandlerUnset(_authRequestedHandler, "AuthRequested");
                _authRequestedHandler = value;
                _callInstanceMethod("setAuthRequestedHandler", new AndroidGeckoAuthRequestedCallback(_handleAuthRequested));
            }
            remove {
                if (_authRequestedHandler == value) {
                    _authRequestedHandler = null;
                    _callInstanceMethod("setAuthRequestedHandler", null);
                }
            }
        }

        /// <see cref="IWithDownloads"/>
        public event EventHandler<DownloadChangedEventArgs> DownloadProgressChanged;

        /// <see cref="IWithFileSelection"/>
        public event EventHandler<FileSelectionEventArgs> FileSelectionRequested {
            add {
                _assertSingletonEventHandlerUnset(_fileSelectionHandler, "FileSelectionRequested");
                _fileSelectionHandler = value;
                _callInstanceMethod("setFileSelectionHandler", new AndroidGeckoFileSelectionCallback(_handleFileSelection));
            }
            remove {
                if (_fileSelectionHandler == value) {
                    _fileSelectionHandler = null;
                    _callInstanceMethod("setFileSelectionHandler", null);
                }
            }
        }

        public WebPluginType PluginType { get; } = WebPluginType.AndroidGecko;

        /// <see cref="IWithPopups"/>
        public event EventHandler<PopupRequestedEventArgs> PopupRequested;

        /// <summary>
        /// Event raised when a script in the page calls `window.alert()`.
        /// </summary>
        /// <remarks>
        /// If no handler is attached to this event, then `window.alert()` will return
        /// immediately and the script will continue execution. If a handler is attached to
        /// this event, then script execution will be paused until the event args' `Continue()`
        /// callback is called.
        /// </remarks>
        /// <example>
        /// <code>
        /// await webViewPrefab.WaitUntilInitialized();
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     var androidGeckoWebView = webViewPrefab.WebView as AndroidGeckoWebView;
        ///     androidGeckoWebView.ScriptAlerted += (sender, eventArgs) => {
        ///         Debug.Log("Script alerted: " + eventArgs.Message);
        ///         eventArgs.Continue();
        ///     };
        /// #endif
        /// </code>
        /// </example>
        public event EventHandler<ScriptDialogEventArgs> ScriptAlerted {
            add {
                _assertSingletonEventHandlerUnset(_scriptAlertHandler, "ScriptAlerted");
                _scriptAlertHandler = value;
                _callInstanceMethod("setScriptAlertHandler", new AndroidGeckoStringAndBoolDelegateCallback(_handleScriptAlert));
            }
            remove {
                if (_scriptAlertHandler == value) {
                    _scriptAlertHandler = null;
                    _callInstanceMethod("setScriptAlertHandler", null);
                }
            }
        }

        /// <summary>
        /// Event raised when a script in the page calls `window.confirm()`.
        /// </summary>
        /// <remarks>
        /// If no handler is attached to this event, then `window.confirm()` will return
        /// `false` immediately and the script will continue execution. If a handler is attached to
        /// this event, then script execution will be paused until the event args' `Continue()` callback
        /// is called, and `window.confirm()` will return the value passed to `Continue()`.
        /// </remarks>
        /// <example>
        /// <code>
        /// await webViewPrefab.WaitUntilInitialized();
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     var androidGeckoWebView = webViewPrefab.WebView as AndroidGeckoWebView;
        ///     androidGeckoWebView.ScriptConfirmRequested += (sender, eventArgs) => {
        ///         Debug.Log("Script confirm requested: " + eventArgs.Message);
        ///         eventArgs.Continue(true);
        ///     };
        /// #endif
        /// </code>
        /// </example>
        public event EventHandler<ScriptDialogEventArgs<bool>> ScriptConfirmRequested {
            add {
                _assertSingletonEventHandlerUnset(_scriptConfirmHandler, "ScriptConfirmRequested");
                _scriptConfirmHandler = value;
                _callInstanceMethod("setScriptConfirmHandler", new AndroidGeckoStringAndBoolDelegateCallback(_handleScriptConfirm));
            }
            remove {
                if (_scriptConfirmHandler == value) {
                    _scriptConfirmHandler = null;
                    _callInstanceMethod("setScriptConfirmHandler", null);
                }
            }
        }

        /// <see cref="IWithChangingTexture"/>
        public event EventHandler<EventArgs<Texture2D>> TextureChanged;

        public override Task<bool> CanGoBack() => Task.FromResult(_callInstanceMethod<bool>("canGoBack"));

        public override Task<bool> CanGoForward() => Task.FromResult(_callInstanceMethod<bool>("canGoForward"));

        // Override so that this method still works even if the device doesn't support the OpenGL GL_OES_EGL_image_external extension.
        public override Task<byte[]> CaptureScreenshot() {

            var taskSource = new TaskCompletionSource<byte[]>();
            _callInstanceMethod("captureScreenshot", new AndroidGeckoByteArrayCallback(taskSource.SetResult));
            return taskSource.Task;
        }

        public static void ClearAllData() => _callStaticMethod("clearAllData");

        /// <see cref="IWithFind"/>
        public void ClearFindMatches() => _callInstanceMethod("clearFindMatches");

        public override void Click(int xInPixels, int yInPixels, bool preventStealingFocus = false) {

            _assertPointIsWithinBounds(xInPixels, yInPixels);
            _callInstanceMethod("click", xInPixels, yInPixels, preventStealingFocus);
        }

        public override void Copy() {

            KeyDown("c", KeyModifier.Control);
            KeyUp("c", KeyModifier.Control);
        }

        public override void Cut() {

            KeyDown("x", KeyModifier.Control);
            KeyUp("x", KeyModifier.Control);
        }

        public override void Dispose() {

            _assertValidState();
            AndroidUtils.AssertMainThread("Dispose");
            IsDisposed = true;
            try {
                // Cancel the render if it has been scheduled via GL.IssuePluginEvent().
                WebView_removePointer(_webView.GetRawObject());
                _webView.Call("destroy");
                _webView.Dispose();
            } catch (NullReferenceException) {
                // This can happen if Unity destroys its native representation of _webView
                // as the app is shutting down. This can happen, for example, on the call
                // to _webView.Dispose(), even though _webView was not null directly before.
            }
            Destroy(gameObject);
        }

        /// <summary>
        /// Installs an extension using GeckoView's `WebExtensionController.ensureBuiltIn()` method.
        /// The extension is not re-installed if it's already present and has the same version.
        /// </summary>
        /// <example>
        /// <code>
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     AndroidGeckoWebView.EnsureBuiltInExtension(
        ///         "resource://android/assets/your-extension/",
        ///         "example@example.com"
        ///     );
        /// #endif
        /// </code>
        /// </example>
        /// <param name="uri">Folder where the extension is located. To ensure this folder is inside the APK, only resource://android URIs are allowed.</param>
        /// <param name="id">Extension ID as present in the manifest.json file.</param>
        public static void EnsureBuiltInExtension(string uri, string id) => _callStaticMethod("ensureBuiltInExtension", uri, id);

        public override void ExecuteJavaScript(string javaScript, Action<string> callback) {

            _assertValidState();
            string resultCallbackId = null;
            if (callback != null) {
                resultCallbackId = Guid.NewGuid().ToString();
                _pendingJavaScriptResultCallbacks[resultCallbackId] = callback;
            }
            _callInstanceMethod("executeJavaScript", javaScript, resultCallbackId);
        }

        /// <see cref="IWithFind"/>
        public Task<FindResult> Find(string text, bool forward) {

            _assertValidState();
            var taskSource = new TaskCompletionSource<FindResult>();
            var resultCallbackId = Guid.NewGuid().ToString();
            _pendingFindCallbacks[resultCallbackId] = taskSource.SetResult;
            _callInstanceMethod("find", text, forward, resultCallbackId);
            return taskSource.Task;
        }

        /// <summary>
        /// Gets the native <see href="https://mozilla.github.io/geckoview/javadoc/mozilla-central/org/mozilla/geckoview/GeckoRuntime.html">GeckoRuntime</see>
        /// used by 3D WebView. If the GeckoRuntime hasn't been created yet, calling this method causes it to be created.
        /// </summary>
        /// <remarks>
        /// Warning: Adding code that interacts with the native GeckoRuntime directly
        /// may interfere with 3D WebView's functionality
        /// and vice versa. So, it's highly recommended to stick to 3D WebView's
        /// C# APIs whenever possible and only use GetGeckoRuntime() if
        /// an equivalent C# API doesn't exist.
        /// </remarks>///
        /// <example>
        /// <code>
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     var geckoRuntime = await AndroidGeckoWebView.GetGeckoRuntime();
        ///     var settings = geckoRuntime.Call&lt;AndroidJavaObject&gt;("getSettings");
        ///     var fontSizeScaleFactor = settings.Call&lt;float&gt;("getFontSizeFactor");
        ///     Debug.Log("font size scale factor: " + fontSizeScaleFactor);
        /// #endif
        /// </code>
        /// </example>
        public static Task<AndroidJavaObject> GetGeckoRuntime() {

            var taskSource = new TaskCompletionSource<AndroidJavaObject>();
            _callStaticMethod("getGeckoRuntime", new AndroidGeckoStringAndObjectCallback((_, runtime) => {
                taskSource.SetResult(runtime);
            }));
            return taskSource.Task;
        }

        /// <summary>
        /// Returns the instance's native GeckoSession.
        /// </summary>
        /// <remarks>
        /// Warning: Adding code that interacts with the native GeckoSession directly
        /// may interfere with 3D WebView's functionality
        /// and vice versa. So, it's highly recommended to stick to 3D WebView's
        /// C# APIs whenever possible and only use GetNativeWebView() if
        /// an equivalent C# API doesn't exist.
        /// </remarks>
        /// <example>
        /// <code>
        /// await webViewPrefab.WaitUntilInitialized();
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     var androidGeckoWebView = webViewPrefab.WebView as AndroidGeckoWebView;
        ///     var geckoSession = androidGeckoWebView.GetNativeWebView();
        ///     // Call the GeckoSession.purgeHistory() to purge the back / forward history.
        ///     // https://mozilla.github.io/geckoview/javadoc/mozilla-central/org/mozilla/geckoview/GeckoSession.html#purgeHistory()
        ///     // Most native GeckoSession methods must be called on the Android UI thread.
        ///     AndroidGeckoWebView.RunOnAndroidUIThread(() => {
        ///         geckoSession.Call("purgeHistory");
        ///     });
        /// #endif
        /// </code>
        /// </example>
        public AndroidJavaObject GetNativeWebView() => _callInstanceMethod<AndroidJavaObject>("getNativeWebView");

        // Override so that this method still works even if the device doesn't support the OpenGL GL_OES_EGL_image_external extension.
        public override Task<byte[]> GetRawTextureData() {

            var taskSource = new TaskCompletionSource<byte[]>();
            _callInstanceMethod("getRawTextureData", new AndroidGeckoByteArrayCallback(taskSource.SetResult));
            return taskSource.Task;
        }

        /// <summary>
        /// Gets the serialized session state, which can be used to restore the session at a later time
        /// using RestoreSessionState().
        /// </summary>
        /// <example>
        /// <code>
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     var androidGeckoWebView = webViewPrefab.WebView as AndroidGeckoWebView;
        ///     var serializedSessionState = androidGeckoWebView.GetSessionState();
        /// #endif
        /// </code>
        /// </example>
        public string GetSessionState() => _callInstanceMethod<string>("getSessionState");

        public static void GloballySetUserAgent(bool mobile) => _callStaticMethod("globallySetUserAgent", mobile);

        public static void GloballySetUserAgent(string userAgent) => _callStaticMethod("globallySetUserAgent", userAgent);

        public override void GoBack() => _callInstanceMethod("goBack");

        public override void GoForward() => _callInstanceMethod("goForward");

        public Task Init(int width, int height) => _initAndroid(width, height, null);

        public static AndroidGeckoWebView Instantiate() => new GameObject().AddComponent<AndroidGeckoWebView>();

        /// <see cref="IWithKeyDownAndUp"/>
        public void KeyDown(string key, KeyModifier modifiers) => _callInstanceMethod("keyDown", key, (int)modifiers);

        /// <see cref="IWithKeyDownAndUp"/>
        public void KeyUp(string key, KeyModifier modifiers) => _callInstanceMethod("keyUp", key, (int)modifiers);

        public override void LoadHtml(string html) => _callInstanceMethod("loadHtml", html);

        public override void LoadUrl(string url) => _callInstanceMethod("loadUrl", _transformUrlIfNeeded(url));

        public override void LoadUrl(string url, Dictionary<string, string> additionalHttpHeaders) {

            if (additionalHttpHeaders == null) {
                LoadUrl(url);
            } else {
                var map = AndroidUtils.ToJavaMap(additionalHttpHeaders);
                _callInstanceMethod("loadUrl", _transformUrlIfNeeded(url), map);
            }
        }

        /// <see cref="IWithMovablePointer"/>
        public void MovePointer(Vector2 normalizedPoint, bool pointerLeave = false) {

            var pixelsPoint = _normalizedToPointAssertValid(normalizedPoint);
            _callInstanceMethod("movePointer", pixelsPoint.x, pixelsPoint.y, pointerLeave);
        }

        public override void Paste() {

            KeyDown("v", KeyModifier.Control);
            KeyUp("v", KeyModifier.Control);
        }

        /// <summary>
        /// Pauses processing, media, and rendering for this webview instance
        /// until `Resume()` is called.
        /// </summary>
        /// <example>
        /// <code>
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     var androidGeckoWebView = webViewPrefab.WebView as AndroidGeckoWebView;
        ///     androidGeckoWebView.Pause();
        /// #endif
        /// </code>
        /// </example>
        public void Pause() => _callInstanceMethod("pause");

        /// <summary>
        /// Pauses processing, media, and rendering for all webview instances.
        /// This method is automatically called by the plugin when the application
        /// is paused.
        /// </summary>
        /// <example>
        /// <code>
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     AndroidGeckoWebView.PauseAll();
        /// #endif
        /// </code>
        /// </example>
        public static void PauseAll() => _callStaticMethod("pauseAll");

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerDown(Vector2 point) => _pointerDown(point, MouseButton.Left, 1, false);

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerDown(Vector2 point, PointerOptions options) {

            if (options == null) {
                options = new PointerOptions();
            }
            _pointerDown(point, options.Button, options.ClickCount, options.PreventStealingFocus);
        }

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerUp(Vector2 point) => _pointerUp(point, MouseButton.Left, 1, false);

        /// <see cref="IWithPointerDownAndUp"/>
        public void PointerUp(Vector2 point, PointerOptions options) {

            if (options == null) {
                options = new PointerOptions();
            }
            _pointerUp(point, options.Button, options.ClickCount, options.PreventStealingFocus);
        }

        public override void Reload() => _callInstanceMethod("reload");

        /// <summary>
        /// Manually triggers the audio workaround described [here](https://support.vuplex.com/articles/oculus-quest-app-audio-stops).
        /// In some cases, 3D WebView may fail to automatically detect the audio issue, and
        /// in that scenario, the application can call this method to manually trigger the workaround.
        /// </summary>
        /// <example>
        /// <code>
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     AndroidGeckoWebView.RestoreAudioFocus();
        /// #endif
        /// </code>
        /// </example>
        public static void RestoreAudioFocus() => _callStaticMethod("restoreAudioFocus");

        /// <summary>
        /// Restores a previous session using the serialized state returned by calling GetSessionState() on this webview
        /// or a previous webview.
        /// </summary>
        /// <example>
        /// <code>
        /// await webViewPrefab.WaitUntilInitialized();
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     var androidGeckoWebView = webViewPrefab.WebView as AndroidGeckoWebView;
        ///     androidGeckoWebView.RestoreSessionState(serializedSessionState);
        /// #endif
        /// </code>
        /// </example>
        public void RestoreSessionState(string serializedSessionState) => _callInstanceMethod("restoreSessionState", serializedSessionState);

        /// <summary>
        /// Resumes processing and rendering for all webview instances
        /// after a previous call to `Pause().`
        /// </summary>
        /// <example>
        /// <code>
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     var androidGeckoWebView = webViewPrefab.WebView as AndroidGeckoWebView;
        ///     androidGeckoWebView.Resume();
        /// #endif
        /// </code>
        /// </example>
        public void Resume() => _callInstanceMethod("resume");

        /// <summary>
        /// Resumes processing and rendering for all webview instances
        /// after a previous call to `PauseAll().` This method
        /// is automatically called by the plugin when the application resumes after
        /// being paused.
        /// </summary>
        /// <example>
        /// <code>
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     AndroidGeckoWebView.ResumeAll();
        /// #endif
        /// </code>
        /// </example>
        public static void ResumeAll() => _callStaticMethod("resumeAll");

        /// <summary>
        /// Runs the given function on the Android UI thread.
        /// </summary>
        /// <example>
        /// <code>
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     AndroidWebView.RunOnAndroidUIThread(() => {
        ///         // TODO: Do something on the Android UI thread.
        ///     });
        /// #endif
        /// </code>
        /// </example>
        public static void RunOnAndroidUIThread(Action function) => AndroidUtils.RunOnAndroidUIThread(function);

        public override void Scroll(int x, int y) => _callInstanceMethod("scroll", x, y);

        public override void Scroll(Vector2 normalizedScrollDelta, Vector2 normalizedPoint) {

            var scrollDeltaInPixels = NormalizedToPoint(normalizedScrollDelta);
            var pointInPixels = _normalizedToPointAssertValid(normalizedPoint);
            _callInstanceMethod("scroll", scrollDeltaInPixels.x, scrollDeltaInPixels.y, pointInPixels.x, pointInPixels.y);
        }

        public override void SelectAll() {

            KeyDown("a", KeyModifier.Control);
            KeyUp("a", KeyModifier.Control);
        }

        public override void SendKey(string key) => _callInstanceMethod("sendKey", key);

        /// <see cref="IWithTouch"/>
        public void SendTouchEvent(TouchEvent touchEvent) {

            var pixelsPoint = _normalizedToPointAssertValid(touchEvent.Point);
            _callInstanceMethod(
                "sendTouchEvent",
                touchEvent.TouchID,
                (int)touchEvent.Type,
                pixelsPoint.x,
                pixelsPoint.y,
                touchEvent.RadiusX,
                touchEvent.RadiusY,
                touchEvent.RotationAngle,
                touchEvent.Pressure
            );
        }

        /// <summary>
        /// This method is automatically called by AndroidGeckoWebPlugin.cs on Meta Quest
        /// in order to activate a workaround for an issue where app audio cuts when a browser
        /// is closed or browser audio is played and then stopped.
        /// https://bugzilla.mozilla.org/show_bug.cgi?id=1766545
        /// </summary>
        public static void SetAudioFocusRecoveryEnabled(bool enabled) => _callStaticMethod("setAudioFocusRecoveryEnabled", enabled);

        /// <see cref="IWithMutableAudio"/>
        public void SetAudioMuted(bool muted) => _callInstanceMethod("setAudioMuted", muted);

        public static void SetAutoplayEnabled(bool enabled) => _callStaticMethod("setAutoplayEnabled", enabled);

        public static new void SetCameraAndMicrophoneEnabled(bool enabled) => _callStaticMethod("setCameraAndMicrophoneEnabled", enabled);

        /// <summary>
        /// Sets whether or not web console messages are output to the Android Logcat logs.
        /// The default is `false`. Unlike IWebView.ConsoleMessageLogged, this method includes
        /// console messages from iframes and console message like uncaught errors that aren't
        /// explicitly passed to a log method like console.log().
        /// </summary>
        /// <remarks>
        /// If enabled, Gecko performance may be negatively impacted if content makes heavy use of the console API.</item>
        /// </remarks>
        /// <example>
        /// <code>
        /// void Awake() {
        ///     #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///         AndroidGeckoWebView.SetConsoleOutputEnabled(true);
        ///     #endif
        /// }
        /// </code>
        /// </example>
        public static void SetConsoleOutputEnabled(bool enabled) => _callStaticMethod("setConsoleOutputEnabled", enabled);

        /// <summary>
        /// By default, the Gecko browser engine outputs debug messages to the
        /// Logcat logs, but you can use this method to disable that.
        /// Note that this method can only be called prior to initializing any webviews, so it's recommended to call it from Awake().
        /// </summary>
        /// <example>
        /// <code>
        /// void Awake() {
        ///     #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///         AndroidGeckoWebView.SetDebugLoggingEnabled(false);
        ///     #endif
        /// }
        /// </code>
        /// </example>
        public static void SetDebugLoggingEnabled(bool enabled) => _callStaticMethod("setDebugLoggingEnabled", enabled);

        public override void SetDefaultBackgroundEnabled(bool enabled) {

            if (!enabled) {
                WebViewLogger.LogWarning("IWebView.SetDefaultBackgroundEnabled() was called but will have no effect because the mobile Gecko browser engine doesn't support transparency. For more info, please see this page: https://support.vuplex.com/articles/how-to-make-a-webview-transparent");
            }
        }

        /// <see cref="IWithDownloads"/>
        public void SetDownloadsEnabled(bool enabled) => _callInstanceMethod("setDownloadsEnabled", enabled);

        /// <summary>
        /// Enables WideVine DRM. DRM is disabled by default because it
        /// could potentially be used for tracking.
        /// </summary>
        /// <remarks>
        /// You can verify that DRM is enabled by using the DRM Stream Test
        /// on [this page](https://bitmovin.com/demos/drm).
        /// </remarks>
        /// <example>
        /// <code>
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     AndroidGeckoWebView.SetDrmEnabled(true);
        /// #endif
        /// </code>
        /// </example>
        public static void SetDrmEnabled(bool enabled) => _callStaticMethod("setDrmEnabled", enabled);

        /// <summary>
        /// Sets whether GeckoView's Enterprise Roots feature is enabled. The default is `false`.
        /// When enabled, GeckoView will fetch the third-party root certificates added to the Android OS CA store and will use them internally.
        /// </summary>
        /// <example>
        /// <code>
        /// void Awake() {
        ///     #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///         AndroidGeckoWebView.SetEnterpriseRootsEnabled(true);
        ///     #endif
        /// }
        /// </code>
        /// </example>
        public static void SetEnterpriseRootsEnabled(bool enabled) => _callStaticMethod("setEnterpriseRootsEnabled", enabled);

        public override void SetFocused(bool focused) => _callInstanceMethod("setFocused", focused);

        /// <summary>
        /// By default, web pages cannot access the device's
        /// geolocation via JavaScript, even if the user has granted
        /// the app permission to access location. Invoking `SetGeolocationEnabled(true)` allows
        /// **all web pages** to access the geolocation if the user has
        /// granted the app location permissions via the standard Android permission dialogs.
        /// </summary>
        /// <remarks>
        /// The following Android permissions must be included in the app's AndroidManifest.xml
        /// and also requested by the application at runtime:
        /// - android.permission.ACCESS_COARSE_LOCATION
        /// - android.permission.ACCESS_FINE_LOCATION
        ///
        /// Note that geolocation doesn't work on Meta Quest devices because they lack GPS support.
        /// </remarks>
        /// <example>
        /// <code>
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     AndroidGeckoWebView.SetGeolocationEnabled(true);
        /// #endif
        /// </code>
        /// </example>
        public static void SetGeolocationEnabled(bool enabled) => _callStaticMethod("setGeolocationEnabled", enabled);

        public static void SetIgnoreCertificateErrors(bool ignore) => _callStaticMethod("setIgnoreCertificateErrors", ignore);

        /// <summary>
        /// Gecko automatically sets the browser engine's locale (which is used for things like the Accept-Language request header)
        /// based on the device's system language. However, you can override the locale using this method.
        /// </summary>
        /// <example>
        /// <code>
        /// void Awake() {
        ///     #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///         AndroidGeckoWebView.SetLocales(new string[] {"es-MX", "es-ES"});
        ///     #endif
        /// }
        /// </code>
        /// </example>
        public static void SetLocales(string[] locales) => _callStaticMethod("setLocales", _toJavaArray(locales));

        /// <see cref="IWithNativeJavaScriptDialogs"/>
        public void SetNativeJavaScriptDialogsEnabled(bool enabled) => _callInstanceMethod("setNativeJavaScriptDialogsEnabled", enabled);

        /// <summary>
        /// By default, Gecko uses a pixel density of 1.0, but the application can call this method at startup
        /// to globally set a different pixel density in order to reduce pixelation and make web content appear more crisp.
        /// Note that this method can only be called prior to initializing any webviews, so it's recommended to call it from Awake().
        /// </summary>
        /// <example>
        /// <code>
        /// void Awake() {
        ///     #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///         AndroidGeckoWebView.SetPixelDensity(2.0f);
        ///     #endif
        /// }
        /// </code>
        /// </example>
        public static void SetPixelDensity(float pixelDensity) {

            var error = _callStaticMethod<string>("setPixelDensity", pixelDensity);
            if (error != null) {
                throw new Exception(error);
            }
        }

        /// <see cref="IWithPopups"/>
        public void SetPopupMode(PopupMode popupMode) => _callInstanceMethod("setPopupMode", (int)popupMode);

        public static void SetRemoteDebuggingEnabled(bool enabled) {

            _callStaticMethod("setRemoteDebuggingEnabled", enabled);
            if (enabled) {
                WebViewLogger.Log("Enabling remote debugging for Android Gecko. For instructions, please see https://support.vuplex.com/articles/how-to-debug-web-content#androidgecko.");
            }
        }

        public override void SetRenderingEnabled(bool enabled) {

            _callInstanceMethod("setRenderingEnabled", enabled);
            _renderingEnabled = enabled;
        }

        public static void SetStorageEnabled(bool enabled) => _callStaticMethod("setStorageEnabled", enabled);

        /// <summary>
        /// Sets the android.view.Surface to which the webview renders.
        /// This can be used, for example, to render to a Meta Quest
        /// [OVROverlay](https://developer.oculus.com/reference/unity/1.34/class_o_v_r_overlay).
        /// When the application invokes this method with a valid surface, the webview renders
        /// to that given surface instead of rendering to its original texture surface (so IWebView.Texture
        /// is no longer updated). If the application invokes this method with a null parameter, it causes the webview
        /// to revert back to rendering to its original texture surface.
        /// </summary>
        /// <example>
        /// <code>
        /// await webViewPrefab.WaitUntilInitialized();
        /// var surface = ovrOverlay.externalSurfaceObject;
        /// webViewPrefab.Resize(ovrOverlay.externalSurfaceWidth, ovrOverlay.externalSurfaceHeight);
        /// #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///     var androidGeckoWebView = webViewPrefab.WebView as AndroidGeckoWebView;
        ///     androidGeckoWebView.SetSurface(surface);
        /// #endif
        /// </code>
        /// </example>
        public void SetSurface(IntPtr surface) {

            var surfaceObject = AndroidUtils.ToJavaObject(surface);
            _callInstanceMethod("setSurface", surfaceObject);
        }

        /// <see cref="IWithSettableUserAgent"/>
        public void SetUserAgent(bool mobile) => _callInstanceMethod("setUserAgent", mobile);

        /// <see cref="IWithSettableUserAgent"/>
        public void SetUserAgent(string userAgent) => _callInstanceMethod("setUserAgent", userAgent);

        /// <summary>
        /// Sets Gecko preferences, which can be used to optionally modify the browser engine's settings.
        /// Note that this method can only be called prior to initializing any webviews, so it's recommended to call it from Awake().
        /// </summary>
        /// <remarks>
        /// The engine's current settings can be viewed by loading the url "about:config" in a webview.
        /// The available Gecko preferences aren't well-documented, but the following pages list some of them:
        /// - [libpref's StaticPrefList.yaml](https://dxr.mozilla.org/mozilla-central/source/modules/libpref/init/StaticPrefList.yaml)
        /// - [libpref's all.js](https://dxr.mozilla.org/mozilla-central/source/modules/libpref/init/all.js)
        /// </remarks>
        /// <example>
        /// <code>
        /// void Awake() {
        ///     #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///         AndroidGeckoWebView.SetPreferences(new Dictionary&lt;string, string&gt; {
        ///             ["security.fileuri.strict_origin_policy"] = "false",
        ///             ["formhelper.autozoom"] = "false"
        ///         });
        ///     #endif
        /// }
        /// </code>
        /// </example>
        public static void SetPreferences(Dictionary<string, string> preferences) {

            var preferencesJavaMap = AndroidUtils.ToJavaMap(preferences);
            var succeeded = _callStaticMethod<bool>("setPreferences", preferencesJavaMap);
            _throwAlreadyInitializedExceptionIfNeeded("SetPreferences", succeeded);
        }

        /// <summary>
        /// Passes an external XR context to GeckoVRManager.setExernalContext().
        /// WebXR isn't yet implemented in 3D WebView, but this method is provided
        /// in case others want to attempt to implement it. For more details on implementing
        /// WebXR, please see [this article](https://support.vuplex.com/articles/webxr).
        /// Note that this method can only be called prior to initializing any webviews, so it's recommended to call it from Awake().
        /// </summary>
        /// <example>
        /// <code>
        /// void Awake() {
        ///     #if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR
        ///         AndroidGeckoWebView.SetXRContext(yourExternalXRContext);
        ///     #endif
        /// }
        /// </code>
        /// </example>
        public static void SetXRContext(IntPtr externalXRContext) {

            var succeeded = _callStaticMethod<bool>("setXRContext", (long)externalXRContext);
            _throwAlreadyInitializedExceptionIfNeeded("SetXRContext", succeeded);
        }

        public override void StopLoad() => _callInstanceMethod("stopLoad");

        public override void ZoomIn() => _callInstanceMethod("zoomIn");

        public override void ZoomOut() => _callInstanceMethod("zoomOut");

    #region Non-public members
        EventHandler<AuthRequestedEventArgs> _authRequestedHandler;
        static AndroidJavaClass _class = new AndroidJavaClass(FULL_CLASS_NAME);
        internal const string DllName = "VuplexWebViewAndroidGecko";
        EventHandler<FileSelectionEventArgs> _fileSelectionHandler;
        const string FULL_CLASS_NAME = "com.vuplex.webview.gecko.GeckoWebView";
        Dictionary<string, Action<FindResult>> _pendingFindCallbacks = new Dictionary<string, Action<FindResult>>();
        EventHandler<ScriptDialogEventArgs> _scriptAlertHandler;
        EventHandler<ScriptDialogEventArgs<bool>> _scriptConfirmHandler;
        readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();
        AndroidJavaObject _webView;

        void _callInstanceMethod(string methodName, params object[] args) {

            _assertValidState();
            AndroidUtils.AssertMainThread(methodName);
            _webView.Call(methodName, args);
        }

        TReturn _callInstanceMethod<TReturn>(string methodName, params object[] args) {

            _assertValidState();
            AndroidUtils.AssertMainThread(methodName);
            return _webView.Call<TReturn>(methodName, args);
        }

        static void _callStaticMethod(string methodName, params object[] args) {

            AndroidUtils.AssertMainThread(methodName);
            _class.CallStatic(methodName, args);
        }

        static TReturn _callStaticMethod<TReturn>(string methodName, params object[] args) {

            AndroidUtils.AssertMainThread(methodName);
            return _class.CallStatic<TReturn>(methodName, args);
        }

        protected override Material _createMaterial() => AndroidUtils.CreateAndroidMaterial();

        protected override Task<Texture2D> _createTexture(int width, int height) {

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan) {
                return base._createTexture(width, height);
            }
            return AndroidOpenGLTextureCreator.GetInstance(WebView_getCreateOpenGLTextureFunction()).CreateTexture(width, height);
        }

        protected override void _destroyNativeTexture(IntPtr nativeTexture) => VulkanDelayedTextureDestroyer.GetInstance(WebView_destroyVulkanTexture).DestroyTexture(nativeTexture);

        void _handleAuthRequested(string host, bool isProxy, Action<string, string> credentialsCallback, Action cancelCallback) {

            var eventArgs = new AuthRequestedEventArgs(host, isProxy, credentialsCallback, cancelCallback);
            _authRequestedHandler(this, eventArgs);
        }

        // Invoked by the native plugin.
        void HandleDownloadProgressChanged(string serializedMessage) {

            DownloadProgressChanged?.Invoke(this, DownloadMessage.FromJson(serializedMessage).ToEventArgs());
        }

        void _handleFileSelection(string serializedMessage, Action<string[]> continueCallback, Action cancelCallback) {

            var message = FileSelectionMessage.FromJson(serializedMessage);
            var eventArgs = new FileSelectionEventArgs(
                message.AcceptFilters,
                message.MultipleAllowed,
                continueCallback,
                cancelCallback
            );
            _fileSelectionHandler(this, eventArgs);
        }

        // Invoked by the native plugin.
        void HandleFindResult(string serializedResult) {

            var segments = serializedResult.Split(new char[]{','});
            var resultCallbackId = segments[0];
            var matchCount = int.Parse(segments[1]);
            var currentMatchIndex = int.Parse(segments[2]);
            var callback = _pendingFindCallbacks[resultCallbackId];
            _pendingFindCallbacks.Remove(resultCallbackId);
            callback(new FindResult {
                MatchCount = matchCount,
                CurrentMatchIndex = currentMatchIndex
            });
        }

        void _handlePopup(string url, AndroidJavaObject session) {

            if (PopupRequested == null) {
                return;
            }
            if (session == null) {
                PopupRequested?.Invoke(this, new PopupRequestedEventArgs(url, null));
                return;
            }
            ThreadDispatcher.RunOnMainThread(async () => {
                var popupWebView = Instantiate();
                await popupWebView._initAndroid(Size.x, Size.y, session);
                PopupRequested?.Invoke(this, new PopupRequestedEventArgs(url, popupWebView));
            });
        }

        void _handleScriptAlert(string message, Action<bool> continueCallback) {

            _scriptAlertHandler(this, new ScriptDialogEventArgs(message, () => continueCallback(true)));
        }

        void _handleScriptConfirm(string message, Action<bool> continueCallback) {

            _scriptConfirmHandler(this, new ScriptDialogEventArgs<bool>(message, continueCallback));
        }

        // Invoked by the native plugin.
        protected override void HandleTextureChanged(string textureString) {

            base.HandleTextureChanged(textureString);
            #if !UNITY_2022_1_OR_NEWER
                // On Android, HandleTextureChanged() is only used for Vulkan.
                // See the comments in IWithChangingTexture.cs for details.
                Texture = Texture2D.CreateExternalTexture(
                    Size.x,
                    Size.y,
                    TextureFormat.RGBA32,
                    false,
                    false,
                    _currentNativeTexture
                );
                TextureChanged?.Invoke(this, new EventArgs<Texture2D>(Texture));
            #endif
        }

        async Task _initAndroid(int width, int height, AndroidJavaObject popupSession) {

            var vulkanEnabled = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan;
            if (vulkanEnabled && !WebView_deviceHasRequiredVulkanExtension()) {
                AndroidUtils.ThrowVulkanExtensionException();
            }
            var task = await _initBase(width, height, asyncInit: true);
            try {
                _webView = new AndroidJavaObject(
                    FULL_CLASS_NAME,
                    gameObject.name,
                    vulkanEnabled ? 0 : Texture.GetNativeTexturePtr().ToInt32(),
                    width,
                    height,
                    SystemInfo.graphicsMultiThreaded,
                    vulkanEnabled,
                    XRSettings.enabled,
                    new AndroidGeckoStringAndObjectCallback(_handlePopup),
                    popupSession
                );
            } catch (AndroidJavaException ex) {
                if (ex.Message.Contains("trial")) {
                    throw new TrialExpiredException(ex.Message);
                }
                throw ex;
            }
            await task;
        }

        // Start the coroutine from OnEnable so that the coroutine
        // is restarted if the object is deactivated and then reactivated.
        void OnEnable() {

            if (SystemInfo.graphicsMultiThreaded && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan) {
                StartCoroutine(_renderPluginOncePerFrame());
            }
        }

        void _pointerDown(Vector2 normalizedPoint, MouseButton mouseButton, int clickCount, bool preventStealingFocus) {

            var pixelsPoint = _normalizedToPointAssertValid(normalizedPoint);
            _callInstanceMethod("pointerDown", pixelsPoint.x, pixelsPoint.y, (int)mouseButton, clickCount, preventStealingFocus);
        }

        void _pointerUp(Vector2 normalizedPoint, MouseButton mouseButton, int clickCount, bool preventStealingFocus) {

            var pixelsPoint = _normalizedToPointAssertValid(normalizedPoint);
            _callInstanceMethod("pointerUp", pixelsPoint.x, pixelsPoint.y, (int)mouseButton, clickCount, preventStealingFocus);
        }

        IEnumerator _renderPluginOncePerFrame() {

            while (true) {
                yield return _waitForEndOfFrame;

                if (!_renderingEnabled || IsDisposed || _webView == null) {
                    continue;
                }
                var nativeWebViewPtr = _webView.GetRawObject();
                if (nativeWebViewPtr != IntPtr.Zero) {
                    int pointerId = WebView_depositPointer(nativeWebViewPtr);
                    GL.IssuePluginEvent(WebView_getRenderFunction(), pointerId);
                }
            }
        }

        protected override void _resize() => _callInstanceMethod("resize", Size.x, Size.y);

        protected override void _setConsoleMessageEventsEnabled(bool enabled) => _callInstanceMethod("setConsoleMessageEventsEnabled", enabled);

        protected override void _setFocusedInputFieldEventsEnabled(bool enabled) => _callInstanceMethod("setFocusedInputFieldEventsEnabled", enabled);

        static void _throwAlreadyInitializedExceptionIfNeeded(string methodName, bool methodSucceeded) {

            if (!methodSucceeded) {
                throw new InvalidOperationException($"Unable to execute AndroidGeckoWebView.{methodName}() because a webview has already been initialized. {methodName}() can only be called prior to initializing any webviews.");
            }
        }

        private static AndroidJavaObject _toJavaArray(string[] values) {

            AndroidJavaClass arrayClass  = new AndroidJavaClass("java.lang.reflect.Array");
            AndroidJavaObject arrayObject = arrayClass.CallStatic<AndroidJavaObject>("newInstance", new AndroidJavaClass("java.lang.String"), values.Length);
            for (int i = 0; i < values.Length; i++) {
                arrayClass.CallStatic("set", arrayObject, i, new AndroidJavaObject("java.lang.String", values[i]));
            }
            return arrayObject;
        }

        [DllImport(DllName)]
        static extern IntPtr WebView_getCreateOpenGLTextureFunction();

        [DllImport(DllName)]
        static extern int WebView_depositPointer(IntPtr pointer);

        [DllImport(DllName)]
        static extern void WebView_destroyVulkanTexture(IntPtr texture);

        [DllImport(DllName)]
        static extern bool WebView_deviceHasRequiredVulkanExtension();

        [DllImport(DllName)]
        static extern IntPtr WebView_getRenderFunction();

        [DllImport(DllName)]
        static extern void WebView_removePointer(IntPtr pointer);
    #endregion

    #region Obsolete APIs
        // Added in v3.9, removed in v3.12.
        [Obsolete("AndroidGeckoWebView.DownloadRequested has been removed. Please use the new IWithDownloads interface instead: https://developer.vuplex.com/webview/IWithDownloads", true)]
        public event EventHandler DownloadRequested;

        // Removed in v4.0.
        [Obsolete("AndroidGeckoWebView.EnableRemoteDebugging has been removed. Please use Web.EnableRemoteDebugging() or AndroidGeckoWebView.SetRemoteDebuggingEnabled() instead.", true)]
        public static void EnableRemoteDebugging() {}

        // Added in v3.3, deprecated in v4.0.
        [Obsolete("AndroidGeckoWebView.SetAudioAndVideoCaptureEnabled() is now deprecated. Please switch to Web.SetCameraAndMicrophoneEnabled(): https://developer.vuplex.com/webview/Web#SetCameraAndMicrophoneEnabled")]
        public static void SetAudioAndVideoCaptureEnabled(bool enabled) => SetCameraAndMicrophoneEnabled(enabled);

        // Deprecated in v4.3.2.
        [Obsolete("AndroidGeckoWebView.SetGeolocationPermissionEnabled() has been renamed to SetGeolocationEnabled(). Please use AndroidGeckoWebView.SetGeolocationEnabled() instead.")]
        public static void SetGeolocationPermissionEnabled(bool enabled) => SetGeolocationEnabled(enabled);

        // Added in v3.3, removed in v3.17.2.
        [Obsolete("AndroidGeckoWebView.SetUserPreferences() has been removed because Mozilla changed the way that preferences are passed to GeckoView. Please use AndroidGeckoWebView.SetPreferences() instead: https://developer.vuplex.com/webview/AndroidGeckoWebView#SetPreferences", true)]
        public static void SetUserPreferences(string preferencesJavaScript) {}
    #endregion
    }
}
#else
namespace Vuplex.WebView {
    [System.Obsolete("To use the AndroidGeckoWebView class, you must use the directive `#if UNITY_ANDROID && !UNITY_EDITOR` like described here: https://support.vuplex.com/articles/how-to-call-platform-specific-apis#android . Note: AndroidGeckoWebView isn't actually obsolete. This compiler error just reports it's obsolete because 3D WebView generated the error with System.ObsoleteAttribute.", true)]
    public class AndroidGeckoWebView {}
}
#endif
