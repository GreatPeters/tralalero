using TMPro;
using UnityEngine;
using System.Collections.Generic;
using IndianOceanAssets.ShooterSurvival;

// Ambient auction, with a clear playable aisle. These people are not enemies or a shooting wall.
public sealed class NoryangjinAuctionActivity : MonoBehaviour
{
    public Animator auctioneer;
    public Animator[] bidders=System.Array.Empty<Animator>();
    public TMP_Text priceBoard;
    public AudioSource voice;
    public AudioClip callClip,soldClip;
    private int beat=-1;
    private float clock;
    private bool running;
    public int GestureChanges{get;private set;}
    public int BidsRaised{get;private set;}
    public int Calls{get;private set;}
    public string soldLot="참치 한 상자";
    private NoryangjinAuctionScatter scatter;
    private readonly Dictionary<Animator,float> gestureUntil=new();
    private void Gesture(Animator animator,string state)
    {
        scatter??=GetComponent<NoryangjinAuctionScatter>();
        if(scatter!=null&&scatter.IsFleeing(animator))return; // fled members are no longer bidding
        if(animator!=null&&animator.HasState(0,Animator.StringToHash(state)))
        {
            if(gestureUntil.TryGetValue(animator,out float until)&&clock<until&&
                (state=="idle"||animator.GetCurrentAnimatorStateInfo(0).IsName(state)))return;
            if(state=="idle"&&!animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).IsName("idle"))return;
            animator.CrossFadeInFixedTime(state,.14f,0,0);GestureChanges++;
            if(state=="bid"||state=="call")gestureUntil[animator]=clock+(state=="bid"?3.8f:7.5f);
            if(state=="bid")BidsRaised++;else if(state=="call")Calls++;
        }
    }
    private void Update()
    {
        var d=NoryangjinRevampDirector.Active;
        bool active=d!=null&&d.Running&&d.Player!=null&&TimeManager.isGameRunning&&Vector3.Distance(d.Player.transform.position,transform.position)<70;
        if(!active)
        {if(running){clock=0;beat=-1;gestureUntil.Clear();if(voice!=null)voice.Stop();}running=false;return;}
        running=true;clock+=Time.deltaTime*TimeManager.timeFactor;
        int next=Mathf.FloorToInt(clock/1.65f);
        if(next==beat)return;beat=next;
        int turn=beat%6;
        for(int i=0;i<bidders.Length;i++)Gesture(bidders[i],turn<5&&i==beat%Mathf.Max(1,bidders.Length)?"bid":"idle");
        if(turn==0||turn==3)Gesture(auctioneer,"call");
        if(priceBoard!=null)priceBoard.text=turn==5?$"{soldLot}\n<color=#F9D16C>낙찰 55,000원</color>":$"활어 경매 진행 중\n<color=#BDF5EF>{30000+turn*5000:N0}원</color>";
        if(NoryangjinRevampDirector.VoicesEnabled&&voice!=null&&(turn==0||turn==5))
        {voice.clip=turn==5?soldClip:callClip;if(voice.clip!=null)voice.Play();}
    }
}
