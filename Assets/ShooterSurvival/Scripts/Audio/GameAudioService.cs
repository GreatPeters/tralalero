using System;
using System.Collections;
using IndianOceanAssets.ShooterSurvival;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameSound
{
    Click, Open, Close, Tab, Denied, Coin, Upgrade, Equip, Shot, EnemyHit, EnemyDeath,
    PlayerHit, PlayerDeath, Knife, Throw, Missile, Explosion, Warning, Bonus, Heal,
    Pause, Resume, Chapter, Victory, Defeat, Holdout
}

public sealed class GameAudioService : MonoBehaviour
{
    public const int VoiceLimit = 12;
    public static GameAudioService Instance { get; private set; }
    public static readonly string[] ClipNames = { "click", "open", "close", "tab", "denied", "coin", "upgrade", "equip", "shot", "enemy_hit", "enemy_death", "player_hit", "player_death", "knife", "throw", "missile", "explosion", "warning", "bonus", "heal", "pause", "resume", "chapter", "victory", "defeat", "holdout" };
    private AudioClip[] clips;
    private AudioClip harbor, traffic;
    private AudioSource music, ambience;
    private readonly AudioSource[] voices = new AudioSource[VoiceLimit];
    private readonly double[] nextAllowed = new double[ClipNames.Length];
    private readonly double[] started = new double[VoiceLimit];
    private readonly int[] priorities = new int[VoiceLimit];
    private bool focused = true, applicationPaused;
    private Transform listener;
    private int variation;
    private bool ready, musicStarted, ambienceStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null) new GameObject("Game Audio").AddComponent<GameAudioService>();
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        clips = new AudioClip[ClipNames.Length];
        for (int i=0;i<clips.Length;i++) clips[i]=Resources.Load<AudioClip>("Audio/Mobile/"+ClipNames[i]);
        music=Source("Music");music.loop=true;music.priority=10;music.volume=.26f;
        music.clip=Resources.Load<AudioClip>("Audio/Mobile/music");
        ambience=Source("Ambience");ambience.loop=true;ambience.priority=180;ambience.volume=.12f;
        harbor=Resources.Load<AudioClip>("Audio/Mobile/harbor");traffic=Resources.Load<AudioClip>("Audio/Mobile/traffic");
        for(int i=0;i<VoiceLimit;i++) voices[i]=Source("Effect "+i);
        SceneManager.sceneLoaded+=SceneLoaded;
    }
    private IEnumerator Start()
    {
        yield return null; // Allow the scene's persisted master mute/volume to initialize first.
        ready = true;
        if (focused && !applicationPaused) ResumeLoops();
    }
    private AudioSource Source(string label)
    {
        var child=new GameObject(label);child.transform.SetParent(transform,false);
        var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.dopplerLevel=0;
        source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=6;source.maxDistance=42;
        return source;
    }
    private void SceneLoaded(Scene scene,LoadSceneMode mode)
    {
        listener=Camera.main!=null?Camera.main.transform:null;
        foreach(var source in voices) source.Stop();
        Array.Clear(nextAllowed,0,nextAllowed.Length);
        ambience.Stop(); ambienceStarted = false;
        ambience.clip=scene.name=="HighWay"?traffic:scene.name=="Noryangjin_MapTool_Mode_SR18"?harbor:null;
        if(focused&&!applicationPaused) ResumeLoops();
    }
    private void Update()
    {
        float target=OpeningStoryUI.IsBlockingGameplay?.18f:TimeManager.isGameRunning?.27f:.21f;
        music.volume=Mathf.MoveTowards(music.volume,target,Time.unscaledDeltaTime*.3f);
        ambience.volume=Mathf.MoveTowards(ambience.volume,TimeManager.isGameRunning?.12f:.07f,Time.unscaledDeltaTime*.15f);
    }
    public static float Cooldown(GameSound sound) => sound switch {
        GameSound.Shot=>.12f,GameSound.EnemyHit=>.065f,GameSound.EnemyDeath=>.12f,
        GameSound.PlayerHit=>.16f,GameSound.Coin=>.07f,GameSound.Warning=>.45f,
        GameSound.Knife=>.18f,GameSound.Explosion=>.18f,_=>.045f };
    public static int Priority(GameSound sound) => sound switch {
        GameSound.PlayerDeath or GameSound.Warning or GameSound.Victory or GameSound.Defeat=>3,
        GameSound.PlayerHit or GameSound.Upgrade or GameSound.Chapter or GameSound.Holdout=>2,
        GameSound.Shot or GameSound.EnemyHit=>0,_=>1 };
    public static void Play(GameSound sound) { if(Application.isPlaying&&Instance!=null)Instance.Emit(sound,Vector3.zero,false); }
    public static void PlayAt(GameSound sound,Vector3 position) { if(Application.isPlaying&&Instance!=null)Instance.Emit(sound,position,true); }
    private void Emit(GameSound sound,Vector3 position,bool spatial)
    {
        int id=(int)sound;
        if(!focused||applicationPaused||id<0||id>=clips.Length||clips[id]==null||AudioListener.volume<=0)return;
        double now=AudioSettings.dspTime;
        if(now<nextAllowed[id])return;
        if(spatial&&listener!=null&&(position-listener.position).sqrMagnitude>42*42)return;
        int selected=-1,priority=Priority(sound);
        for(int i=0;i<VoiceLimit;i++)if(!voices[i].isPlaying){selected=i;break;}
        if(selected<0)
            for(int i=0;i<VoiceLimit;i++)
                if(priorities[i]<=priority&&(selected<0||priorities[i]<priorities[selected]||priorities[i]==priorities[selected]&&started[i]<started[selected]))selected=i;
        if(selected<0)return;
        nextAllowed[id]=now+Cooldown(sound);started[selected]=now;priorities[selected]=priority;
        var source=voices[selected];source.Stop();source.clip=clips[id];source.transform.position=spatial?position:Vector3.zero;
        source.spatialBlend=spatial?.4f:0;source.priority=150-priority*35;
        source.volume=sound==GameSound.Shot?.22f:sound==GameSound.EnemyHit?.4f:.62f;
        source.pitch=sound==GameSound.Shot||sound==GameSound.EnemyHit||sound==GameSound.Coin?1f+((variation++%7)-3)*.016f:1;
        source.Play();
    }
    private void OnApplicationFocus(bool value){focused=value;ApplyFocus();}
    private void OnApplicationPause(bool value){applicationPaused=value;ApplyFocus();}
    private void ApplyFocus()
    {
        if(!focused||applicationPaused)
        {
            if(music!=null)music.Pause();
            if(ambience!=null)ambience.Pause();
            foreach(var source in voices)if(source!=null)source.Stop();
        }
        else ResumeLoops();
    }
    private void ResumeLoops()
    {
        if (!ready) return;
        if (music != null && music.clip != null)
        {
            if (musicStarted) music.UnPause();
            else { music.Play(); musicStarted = true; }
        }
        if (ambience != null && ambience.clip != null)
        {
            if (ambienceStarted) ambience.UnPause();
            else { ambience.Play(); ambienceStarted = true; }
        }
    }
    private void OnDestroy()
    {
        ready=false;
        SceneManager.sceneLoaded-=SceneLoaded;
        if(Instance==this)Instance=null;
    }
}
