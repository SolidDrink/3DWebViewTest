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
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView {

    class AndroidGeckoWebPlugin : MonoBehaviour, IWebPlugin {

        public ICookieManager CookieManager { get; } = null;

        public static AndroidGeckoWebPlugin Instance {
            get {
                if (_instance == null) {
                    _instance = new GameObject("AndroidGeckoWebPlugin").AddComponent<AndroidGeckoWebPlugin>();
                    DontDestroyOnLoad(_instance.gameObject);
                    // Enable audio focus recovery for all Quest headsets (1, 2, Pro).
                    if (AndroidUtils.DeviceIsMetaQuest()) {
                        AndroidGeckoWebView.SetAudioFocusRecoveryEnabled(true);
                    }
                }
                return _instance;
            }
        }

        public WebPluginType Type { get; } = WebPluginType.AndroidGecko;

        public void ClearAllData() => AndroidGeckoWebView.ClearAllData();

        // Deprecated
        public void CreateMaterial(Action<Material> callback) => callback(AndroidUtils.CreateAndroidMaterial());

        public IWebView CreateWebView() => AndroidGeckoWebView.Instantiate();

        public void EnableRemoteDebugging() => AndroidGeckoWebView.SetRemoteDebuggingEnabled(true);

        public void SetAutoplayEnabled(bool enabled) => AndroidGeckoWebView.SetAutoplayEnabled(enabled);

        public void SetCameraAndMicrophoneEnabled(bool enabled) => AndroidGeckoWebView.SetCameraAndMicrophoneEnabled(enabled);

        public void SetIgnoreCertificateErrors(bool ignore) => AndroidGeckoWebView.SetIgnoreCertificateErrors(ignore);

        public void SetStorageEnabled(bool enabled) => AndroidGeckoWebView.SetStorageEnabled(enabled);

        public void SetUserAgent(bool mobile) => AndroidGeckoWebView.GloballySetUserAgent(mobile);

        public void SetUserAgent(string userAgent) => AndroidGeckoWebView.GloballySetUserAgent(userAgent);

        static AndroidGeckoWebPlugin _instance;

        // Automatically pause web processing and media playback
        // when the app is paused and resume it when the app is resumed.
        void OnApplicationPause(bool isPaused) {

            if (isPaused) {
                AndroidGeckoWebView.PauseAll();
            } else {
                AndroidGeckoWebView.ResumeAll();
            }
        }
    }
}
#endif
