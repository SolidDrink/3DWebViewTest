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
#if UNITY_ANDROID
#pragma warning disable CS0618
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView.Editor {

    /// <summary>
    /// Build script that applies required settings for Android Gecko.
    /// </summary>
    public class AndroidGeckoBuildScript : IPreprocessBuild {

        public int callbackOrder { get => 0; }

        public void OnPreprocessBuild(BuildTarget buildTarget, string buildPath) {

            if (buildTarget != BuildTarget.Android) {
                return;
            }
            AndroidEditorUtils.PreprocessBuild("3D WebView for Android with Gecko Engine", "Assets/Vuplex/WebView/AndroidGecko/Plugins/proguard-webview-android-gecko.txt", "libVuplexWebViewAndroidGecko.so", false);
            _deleteOldExtensionDirectoryIfNeeded();
        }

        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject) {

            _warnAboutSplitApplicationBinaryIfNeeded(pathToBuiltProject);
        }

        const string OLD_EXTENSION_DIRECTORY_NAME = "vuplex-webview-gecko-extension";

        /// <summary>
        /// AndroidGeckoBuildScript.cs used to copy a vuplex-webview-gecko-extension folder for 3D WebView's
        /// built-in Gecko extension to the Assets/Plugins/Android/assets folder to make Unity include it in
        /// the APK's assets, but Unity 2021.2 removed the ability to include assets like that and now fails
        /// the build if the Assets/Plugins/Android/assets folder exists. So, the extension is now included
        /// in an AAR file, and this method deletes the Assets/Plugins/Android/assets/vuplex-webview-gecko-extension
        /// directory if it exists.
        /// </summary>
        static void _deleteOldExtensionDirectoryIfNeeded() {

            var androidAssetsDirectoryPath = Path.Combine(Application.dataPath, "Plugins", "Android", "assets");
            var oldExtensionDirectoryPath = Path.Combine(androidAssetsDirectoryPath, OLD_EXTENSION_DIRECTORY_NAME);
            if (!Directory.Exists(oldExtensionDirectoryPath)) {
                return;
            }
            // Check if the assets directory contains other files or directories besides the vuplex-webview-gecko-extension folder and meta file.
            var otherFiles = Directory.GetFiles(androidAssetsDirectoryPath)
                                      .ToList()
                                      .Where(file => !(file.EndsWith(OLD_EXTENSION_DIRECTORY_NAME + ".meta") || file.EndsWith(".DS_Store")))
                                      .ToArray();
            var otherDirectories = Directory.GetDirectories(androidAssetsDirectoryPath)
                                            .ToList()
                                            .Where(directory => !directory.EndsWith(OLD_EXTENSION_DIRECTORY_NAME))
                                            .ToArray();
            var numberOfOtherFilesAndDirectories = otherFiles.Count() + otherDirectories.Count();
            if (numberOfOtherFilesAndDirectories > 0) {
                // The assets folder contains other files besides 3D WebView's old vuplex-webview-gecko-extension directory,
                // so only remove the vuplex-webview-gecko-extension directory.
                Directory.Delete(oldExtensionDirectoryPath, true);
                return;
            }
            // The assets directory only includes 3D WebView's old vuplex-webview-gecko-extension directory, so
            // the entire assets directory can be deleted.
            Directory.Delete(androidAssetsDirectoryPath, true);
        }

        // https://support.vuplex.com/articles/android-gecko-crash#large-apk
        static void _warnAboutSplitApplicationBinaryIfNeeded(string apkPath) {

            if (!apkPath.EndsWith(".apk")) {
                // The build artifact isn't an APK. This can happen with the Oculus SDK's OVRBuild option,
                // in which case we need to skip this check because apkPath is the path to a directory.
                return;
            }
            if (PlayerSettings.Android.useAPKExpansionFiles) {
                // Split Application Binary" is already enabled.
                return;
            }
            var apkSizeInBytes = new FileInfo(apkPath).Length;
            if (apkSizeInBytes > 1e9) {
                var message = "Your APK is larger than 1 GB, so please enable \"Split Application Binary\" in \"Android Player Settings -> Publishing Settings\" in order to avoid the Gecko issue described here: https://support.vuplex.com/articles/android-gecko-crash#large-apk";
                #if VUPLEX_ANDROID_GECKO_ALLOW_LARGE_APK
                    WebViewLogger.LogWarning(message);
                #else
                    throw new BuildFailedException(message);
                #endif
            }
        }
    }
}
#endif
