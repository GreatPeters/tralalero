using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class ChapterTransitionUI : MonoBehaviour
{
    public VideoPlayer player;
    public RawImage display;
    public TMP_Text title, caption, status;
    public Button skipButton;
    private RenderTexture texture;
    private bool skip, failed;
    public bool IsPresenting { get; private set; }

    public IEnumerator Present(VideoClip clip, string heading, string text, int clearJewels = 0)
    {
        Release(); skip = failed = false;
        if (clip == null || player == null || display == null) yield break;
        gameObject.SetActive(true); IsPresenting = true;
        if (title != null) title.text = heading;
        if (caption != null) caption.text = text;
        if (status != null) status.text = clearJewels > 0 ? $"완주 보상  보석 +{clearJewels}" : "";
        if (skipButton != null) skipButton.interactable = true;
        try
        {
            texture = new RenderTexture((int)clip.width, (int)clip.height, 0) { name = "Chapter transition" };
            texture.Create(); display.texture = texture;
            var fit = display.GetComponent<AspectRatioFitter>();
            if (fit != null) { fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = (float)clip.width / clip.height; }
            player.playOnAwake = false; player.isLooping = false; player.waitForFirstFrame = true;
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.clip = clip; player.targetTexture = texture; player.errorReceived += Failed;
            player.Prepare();
            float deadline = Time.realtimeSinceStartup + 12;
            while (!player.isPrepared && !skip && !failed && Time.realtimeSinceStartup < deadline) yield return null;
            if (!player.isPrepared || skip || failed) yield break;
            player.Play();
            deadline = Time.realtimeSinceStartup + (float)clip.length + 8;
            // isPlaying may be false for the first decoder tick; wait for a frame first.
            while (player.frame < 0 && !skip && !failed && Time.realtimeSinceStartup < deadline) yield return null;
            while (!skip && !failed && Time.realtimeSinceStartup < deadline &&
                   (player.isPlaying || player.time < clip.length - .1)) yield return null;
        }
        finally
        {
            Release();
            if (this != null) gameObject.SetActive(false);
        }
    }
    public void Skip() => skip = true;
    public void ShowLoading()
    {
        Release(); gameObject.SetActive(true);
        if (title != null) title.text = "다음 챕터";
        if (caption != null) caption.text = "다음 길을 준비하고 있습니다.";
        if (status != null) status.text = "이동 중";
        if (skipButton != null) skipButton.interactable = false;
    }
    private void Failed(VideoPlayer _, string message) { failed = true; Debug.LogWarning("Chapter movie: " + message, this); }
    private void Release()
    {
        IsPresenting = false;
        var released = texture; texture = null;
        if (player != null) { player.errorReceived -= Failed; player.Stop(); player.targetTexture = null; }
        if (display != null) display.texture = null;
        if (released != null) { released.Release(); Destroy(released); }
    }
    private void OnDisable() { skip = true; Release(); }
    private void OnDestroy() => Release();
}
