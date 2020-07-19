//Copy-Pasted From: 
//https://forum.unity.com/threads/disable-auto-refresh-when-in-playmode.136325/
//Code from 2016 so decent chances something's changed significantly.
namespace UnityEditor.Utility
{
    /// <summary>
    /// Disables the editor Auto Refresh asset import behavior when in playing mode and restores it on return to edit mode.
    /// </summary>
    [InitializeOnLoad]
    public static class DisableAssetAutoImportOnPlay
    {
        const string kKeyOfAutoRefresh = "kAutoRefresh";
        static bool IsAutoRefreshOn;

        static bool HasAutoRefreshKeyword
        {
            get { return EditorPrefs.HasKey(kKeyOfAutoRefresh); }
        }

        /// <summary>
        /// Due to InitializeOnLoadAttribute, this static Constructor will be called when this editor assembly loads (on startup and on AppDomain restart after compile).
        /// </summary>
        static DisableAssetAutoImportOnPlay()
        {
            EditorApplication.playModeStateChanged += OnEditorApplicationPlayModeStateChanged;
        }

        /// <summary>
        /// Called when the EditorApplication.playModeStateChanged event fires
        /// </summary>
        /// <param name="playingState"></param>
        static void OnEditorApplicationPlayModeStateChanged(PlayModeStateChange playingState)
        {
            switch (playingState)
            {
                // Called the moment after the user presses the Play button.
                case PlayModeStateChange.ExitingEditMode:
                    break;

                // Called when the initial scene is loaded and first rendered, after ExitingEditMode..
                case PlayModeStateChange.EnteredPlayMode:
                    if (HasAutoRefreshKeyword)
                    {
                        // record AutoRefresh status
                        IsAutoRefreshOn = EditorPrefs.GetBool(kKeyOfAutoRefresh);

                        // switch status when "Auto Refresh" is active at beginning
                        if (IsAutoRefreshOn)
                            EditorPrefs.SetBool(kKeyOfAutoRefresh, false);
                    }

                    break;

                // Called the moment after the user presses the Stop button.
                //case PlayModeStateChange.ExitingPlayMode:
                //    break;

                // Called after the current scene is unloaded, after ExitingPlayMode.
                case PlayModeStateChange.EnteredEditMode:
                    if (HasAutoRefreshKeyword)
                    {
                        // switch status when "Auto Refresh" is active at beginning
                        if (IsAutoRefreshOn)
                            EditorPrefs.SetBool(kKeyOfAutoRefresh, true);
                    }

                    break;
            }
        }
    }
}
