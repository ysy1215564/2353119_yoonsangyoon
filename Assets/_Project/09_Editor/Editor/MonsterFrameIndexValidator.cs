using UnityEditor;
using UnityEngine;

/// <summary>
/// Safety net for the "프레임 배열 크기 + 짝을 이루는 판정 프레임 번호" pitfall family
/// (see [[monster-c-frame-index-pitfall]]) - resizing a monster's Sprite[] frame array without
/// updating the paired 1-based "몇 번째 프레임에 판정" index field breaks that action with NO
/// runtime error, just silence (the timed check simply never matches any real frame index during
/// the animation's loop). Added 2026-08-17 alongside the multi-stage expansion feature - once
/// reskinning with a DIFFERENT frame count than the original becomes a routine self-service
/// workflow, students will hit this far more often than the one professor who already knew to
/// watch for it.
///
/// This only WARNS (never auto-fixes or blocks) - the correct fix depends on animation intent
/// the tool can't know (should the swing/effect/projectile happen earlier, or does the new art
/// just need one more frame?), so this only makes the mistake visible in the Console instead of
/// silently doing nothing.
/// </summary>
public static class MonsterFrameIndexValidator
{
    /// <summary>
    /// Checks that a 1-based "몇 번째 프레임에 X" index field falls within [1, spriteArray.Length].
    /// Skips silently if the sprite array is empty (art not dropped in yet - nothing meaningful
    /// to validate). <paramref name="label"/> is just for the warning message (e.g.
    /// "Mold3/attack1ProjectileFrame") - doesn't need to match the actual field name exactly.
    /// </summary>
    public static void WarnIfOutOfRange(SerializedObject so, string spriteArrayPropertyName, string oneBasedIndexPropertyName, string label)
    {
        var spriteArrayProperty = so.FindProperty(spriteArrayPropertyName);
        var indexProperty = so.FindProperty(oneBasedIndexPropertyName);
        if (spriteArrayProperty == null || indexProperty == null)
        {
            return; // field renamed/removed on the script side - not this tool's job to catch that
        }

        var frameCount = spriteArrayProperty.arraySize;
        if (frameCount == 0)
        {
            return; // no art dropped in yet for this animation - nothing to validate
        }

        var index = indexProperty.intValue;
        if (index >= 1 && index <= frameCount)
        {
            return; // in range, nothing to warn about
        }

        Debug.LogWarning(
            $"[프레임 판정 범위 경고] {label} = {index}인데 {spriteArrayPropertyName}에는 프레임이 {frameCount}장뿐입니다. " +
            $"이 상태로는 해당 동작(공격 타격/이펙트/투사체 등)이 재생 중 한 번도 안 일어날 수 있습니다 - " +
            $"인스펙터에서 {oneBasedIndexPropertyName} 값을 1~{frameCount} 사이로 맞춰주세요.");
    }
}
