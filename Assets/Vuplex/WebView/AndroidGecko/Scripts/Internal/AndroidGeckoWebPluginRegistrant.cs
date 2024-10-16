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
using UnityEngine;

namespace Vuplex.WebView.Internal {

    /// <summary>
    /// Registers the Android Gecko web plugin.
    /// </summary>
    /// <remarks>
    /// There's a weird Unity bug where a method decorated with `RuntimeInitializeOnLoadMethod`
    /// isn't called on Android if it's nested within a block that's conditionally included
    /// using the `UNITY_EDITOR` directive, which the AndroidWebPlugin class uses.
    /// This class works around that issue by using the `UNITY_EDITOR` directive *inside*
    /// the method instead of outside.
    /// </remarks>
    class AndroidGeckoWebPluginRegistrant {

        // Note: This needs to be BeforeSceneLoad because using earlier callbacks (like BeforeSplashScreen),
        //       prevents the AndroidGeckoWebPlugin script's MonoBehaviour methods like OnApplicationPause() from
        //       being called.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void _registerPlugin() {

            #if UNITY_ANDROID
                #if UNITY_EDITOR
                    // Register the mock in the Editor so that WebPluginFactory knows the package is installed.
                    var plugin = MockWebPlugin.Instance;
                #else
                    var plugin = AndroidGeckoWebPlugin.Instance;
                #endif
                WebPluginFactory.RegisterAndroidGeckoPlugin(plugin);
            #endif
        }
    }
}
