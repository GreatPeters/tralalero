using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class OpeningStoryUI : MonoBehaviour
{
    public static OpeningStoryUI Instance { get; private set; }
    private static bool shownThisSession;
    private bool manualOpen;
    public static bool IsBlockingGameplay => Instance != null && Instance.gameObject.activeInHierarchy;
    public Texture2D comic;
    public Texture2D[] pages;
    public RawImage artwork, backdrop, movieDisplay;
    public CanvasGroup pageFade;
    public TMP_Text chapterText, titleText, captionText, nextText;
    public Button previousButton;
    public VideoPlayer video;
    public VideoClip movie;
    public GameObject pageContent;
    public Image pageProgress;
    public Image[] sceneIndicators;
    public Sprite activeSceneSprite, inactiveSceneSprite;
    public bool autoPlayMovie = true;
    private int page;
    private float elapsed;
    private RenderTexture videoTexture;
    private bool seeking;
    private double seekTarget;
    public bool IsMoviePlaying => video != null && video.isPlaying;
    public bool IsSeeking => seeking;
    public int CurrentPage => page;
    public static int MovieSceneCount => moviePageStarts.Length;
    // Installed movie: two8-second Flow clips, followed by two121-frame/24fps clips.
    private static readonly double[] moviePageStarts = { 0d, 8d, 16d, 505d / 24d };
    public static double GetMoviePageStart(int index) => moviePageStarts[Mathf.Clamp(index, 0, 3)];
    public static int GetMoviePageAtTime(double seconds)
    {
        for (int index = moviePageStarts.Length - 1; index > 0; index--)
            if (seconds >= moviePageStarts[index]) return index;
        return 0;
    }
    private readonly string[] titles = { "훔친 신발", "벗을 수 없는 저주", "신이 내건 조건", "노량진으로" };
    private readonly string[] captions = {
        "신단의 신발을 물고,\n상어는 항구를 빠져나갔다.",
        "두 발과 꼬리 끝에 신발이 붙었다.\n아무리 벗으려 해도 떨어지지 않았다.",
        "저주를 풀려면 인간 세상에서\n더 좋은 신발을 찾아 바쳐야 한다.",
        "신발을 고쳐 신고 다시 출발한다.\n첫 목적지는 노량진 수산시장." };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; shownThisSession = false; }
    private void Awake()
    {
        Instance = this;
        var layer = GetComponent<Canvas>();
        if (layer != null) { layer.overrideSorting = true; layer.sortingOrder = 200; }
        if (video == null) return;
        video.loopPointReached += VideoFinished;
        video.prepareCompleted += VideoPrepared;
        video.seekCompleted += VideoSeeked;
        video.frameReady += VideoFrameReady;
        video.errorReceived += VideoFailed;
    }
    private void OnEnable()
    {
        if (Application.isPlaying && autoPlayMovie)
        {
            if (shownThisSession && !manualOpen) { gameObject.SetActive(false); return; }
            shownThisSession = true;
        }
        manualOpen = false;
        StopMovie(); page = 0;
        if (pageContent != null) pageContent.SetActive(true);
        ShowPage();
        if (Application.isPlaying && autoPlayMovie) PlayMovie();
    }
    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (pageFade != null) pageFade.alpha = Mathf.Clamp01(elapsed / .25f);
        if (IsMoviePlaying && !seeking && video.length > 0)
        {
            int next = GetMoviePageAtTime(video.time);
            if (next != page) { page = next; ShowPage(); }
        }
        if (Input.GetKeyDown(KeyCode.Escape)) { Skip(); return; }
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Next();
    }
    public void Next()
    {
        if (seeking) return;
        if (++page >= titles.Length) { Skip(); return; }
        if (videoTexture != null && video.isPrepared && video.length > 0)
        {
            seeking = true;
            seekTarget = GetMoviePageStart(page);
            video.Play();
            video.time = seekTarget;
        }
        ShowPage();
    }
    public void Previous()
    {
        if(seeking||page<=0)return;
        page=PreviousPageIndex(page);
        if(videoTexture!=null&&video!=null&&video.isPrepared&&video.length>0)
        {
            seeking=true;seekTarget=GetMoviePageStart(page);video.Play();video.time=seekTarget;
        }
        ShowPage();
    }
    public static int PreviousPageIndex(int current)=>Mathf.Max(0,current-1);
    private void ShowPage()
    {
        elapsed = 0f;
        if (artwork != null)
        {
            bool separatePage = pages != null && pages.Length == titles.Length && pages[page] != null;
            artwork.texture = separatePage ? pages[page] : comic;
            artwork.uvRect = separatePage ? new Rect(0, 0, 1, 1) : new Rect(0, 1f - (page + 1) * .25f, 1, .25f);
        }
        if (chapterText != null) chapterText.text = $"이야기 {page + 1:00} / {titles.Length:00}";
        if (pageProgress != null) pageProgress.fillAmount = (page + 1f) / titles.Length;
        if (sceneIndicators != null)
            for (int i = 0; i < sceneIndicators.Length; i++)
                if (sceneIndicators[i] != null) sceneIndicators[i].sprite = i == page ? activeSceneSprite : inactiveSceneSprite;
        if (titleText != null) titleText.text = titles[page];
        if (captionText != null) captionText.text = captions[page];
        if (nextText != null) nextText.text = page == titles.Length - 1 ? "여정 시작" : "다음 장면";
        if(previousButton!=null)previousButton.interactable=page>0;
    }
    public void PlayMovie()
    {
        if (videoTexture != null || video == null || movie == null || movieDisplay == null) return;
        var fit = movieDisplay.GetComponent<AspectRatioFitter>();
        if (fit != null)
        {
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = (float)movie.width / movie.height;
        }
        videoTexture = new RenderTexture((int)movie.width, (int)movie.height, 0) { name = "Opening animation" };
        videoTexture.Create();
        var previousTarget=RenderTexture.active;RenderTexture.active=videoTexture;GL.Clear(true,true,Color.black);RenderTexture.active=previousTarget;
        video.clip = movie; video.targetTexture = videoTexture; video.isLooping = false;
        video.sendFrameReadyEvents = true;
        video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        video.playOnAwake = false; video.waitForFirstFrame = true;
        movieDisplay.texture = videoTexture;
        movieDisplay.gameObject.SetActive(false);
        video.Prepare();
    }
    public void ReplayMovie()
    {
        StopMovie(); page = 0; ShowPage(); PlayMovie();
    }
    public void Open()
    {
        if (TimeManager.isGameRunning) return;
        if (gameObject.activeSelf) ReplayMovie();
        else { manualOpen = true; gameObject.SetActive(true); }
    }
    private void VideoPrepared(VideoPlayer source)
    {
        if (!isActiveAndEnabled || videoTexture == null) return;
        if (page > 0) { seeking = true; seekTarget=GetMoviePageStart(page); source.Play();source.time = seekTarget; return; }
        source.Play();
    }
    private void VideoSeeked(VideoPlayer source)
    {
        if (!isActiveAndEnabled || videoTexture == null) return;
        source.Play();
    }
    private void VideoFrameReady(VideoPlayer source, long frame)
    {
        if (!isActiveAndEnabled || videoTexture == null) return;
        if (seeking)
        {
            // WMF may report seek completion before the requested frame is displayed.
            // Keep the requested page and footer stable until that frame arrives.
            if (source.time < seekTarget - .08d || source.time > seekTarget + 1d) return;
            seeking = false;
        }
        movieDisplay.gameObject.SetActive(true);
        if (artwork != null) artwork.gameObject.SetActive(false);
    }
    private void VideoFinished(VideoPlayer _) => Skip();
    private void VideoFailed(VideoPlayer _, string message)
    {
        Debug.LogWarning("Opening video could not play: " + message, this);
        StopMovie(); ShowPage();
    }
    public void Skip()
    {
        StopMovie();
        FindFirstObjectByType<PlayerScript>()?.ResetStartGesture();
        gameObject.SetActive(false);
    }
    private void StopMovie()
    {
        seeking = false;
        var releasedTexture = videoTexture;
        videoTexture = null; // Invalidate callbacks before Stop can change playback state.
        if (video != null) { video.Stop(); video.targetTexture = null; }
        if (movieDisplay != null) { movieDisplay.texture = null; movieDisplay.gameObject.SetActive(false); }
        if (artwork != null) artwork.gameObject.SetActive(true);
        if (releasedTexture == null) return;
        releasedTexture.Release();
        if (Application.isPlaying) Destroy(releasedTexture); else DestroyImmediate(releasedTexture);
    }
    private void OnDisable() => StopMovie();
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (video != null)
        {
            video.loopPointReached -= VideoFinished;
            video.prepareCompleted -= VideoPrepared;
            video.seekCompleted -= VideoSeeked;
            video.frameReady -= VideoFrameReady;
            video.errorReceived -= VideoFailed;
        }
        StopMovie();
    }
}
