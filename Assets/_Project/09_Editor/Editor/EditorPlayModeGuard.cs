using UnityEditor;
using UnityEngine;

/// <summary>
/// Blocks scene-switching editor tools from running while Play Mode is active. Added 2026-08-17
/// after "Sync Player Stats From ActionTest" was run while Play Mode was still going and caused
/// the editor to land on the wrong scene plus a MissingReferenceException (a live Play-mode
/// PlayerActionTestController got orphaned mid `EditorSceneManager.OpenScene` scene-swap).
/// `EditorSceneManager.OpenScene(..., Single)` during Play Mode is not something Unity supports
/// safely - every tool in this project that swaps the open scene to do its work (all the
/// "Create"/"Sync"/"Apply" multi-scene tools) should check this first.
/// </summary>
public static class EditorPlayModeGuard
{
    /// <summary>Returns true (and logs a warning) if Play Mode is active - caller should bail
    /// out immediately without touching any scene.</summary>
    public static bool BlockIfPlaying(string toolName)
    {
        if (!EditorApplication.isPlaying)
        {
            return false;
        }

        Debug.LogWarning($"{toolName}: Play 모드를 먼저 정지(■)하고 다시 실행하세요 - 재생 중에는 씬을 안전하게 바꿀 수 없습니다.");
        return true;
    }
}
