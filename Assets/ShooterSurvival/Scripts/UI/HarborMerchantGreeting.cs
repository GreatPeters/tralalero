using UnityEngine;
using IndianOceanAssets.ShooterSurvival;

public sealed class HarborMerchantGreeting : MonoBehaviour
{
    public Animator animator;
    private bool greeted;
    public bool HasGreeted=>greeted;
    private void Update(){if(!greeted&&TimeManager.isGameRunning)Greet();}
    public void Greet(){if(greeted||animator==null)return;greeted=true;animator.SetTrigger("Greet");}
}
