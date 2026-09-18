using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Brief, non-blocking prompts for the existing four tutorial topics.</summary>
public sealed class CoastalTutorialUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text title, body;
    public Image icon;
    public Sprite[] icons;
    private PlayerScript player;
    private Transform[][] targets;
    private int seen;
    private bool complete;
    private float nextProbe, hideAt, nextHint;
    private static readonly string[] Titles={"좌우로 이동","적을 조준하세요","무기를 바꿔보세요","효과를 확인하세요"};
    private static readonly string[] Bodies={"좌우로 밀어 이동하세요","적을 향해 서면 자동 공격","무기 통을 부수면 무기 변경","빛나는 보너스 쪽으로 지나가세요"};

    private void Start()
    {
        player=FindFirstObjectByType<PlayerScript>();
        complete=PlayerPrefs.GetInt("TutorialDone",0)==1;
        var transforms=FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        targets=new[]{"EnemyTag","BarrelTag","WallTag"}.Select(tag=>transforms.Where(t=>t.gameObject.tag==tag).ToArray()).ToArray();
        panel.SetActive(false);nextHint=Time.unscaledTime+1;
    }
    private void Update()
    {
        if(panel.activeSelf && Time.unscaledTime>=hideAt)Close();
        if(complete||player==null||!TimeManager.isGameRunning||panel.activeSelf||Time.unscaledTime<nextHint||Time.unscaledTime<nextProbe)return;
        nextProbe=Time.unscaledTime+.3f;
        if((seen&1)==0){ShowTopic(0);return;}
        for(int i=1;i<4;i++)
        {
            if((seen&(1<<i))!=0)continue;
            if(targets[i-1].Any(t=>t!=null&&t.gameObject.activeInHierarchy&&IsAheadAndNear(player.transform.position,player.transform.forward,t.position,24)))
            {ShowTopic(i);break;}
        }
    }
    public static bool IsAheadAndNear(Vector3 origin,Vector3 forward,Vector3 point,float distance)
    {
        var delta=point-origin;delta.y=0;forward.y=0;
        return delta.sqrMagnitude<=distance*distance&&Vector3.Dot(delta,forward)>0;
    }
    public void ShowTopic(int index)
    {
        if(index<0||index>=Titles.Length)return;
        title.text=Titles[index];body.text=Bodies[index];icon.sprite=icons[index];
        seen|=1<<index;panel.SetActive(true);hideAt=Time.unscaledTime+2.5f;
        if(seen==15){complete=true;PlayerPrefs.SetInt("TutorialDone",1);PlayerPrefs.Save();}
    }
    public void Close(){panel.SetActive(false);nextHint=Time.unscaledTime+2;}
}
