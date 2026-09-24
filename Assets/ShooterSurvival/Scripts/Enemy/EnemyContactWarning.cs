using TMPro;
using UnityEngine;

namespace IndianOceanAssets.ShooterSurvival
{
    [DisallowMultipleComponent]
    public sealed class EnemyContactWarning : MonoBehaviour
    {
        public const string ResourcePath="CombatFeedback/ContactWarning";
        EnemyScript_space enemy;PlayerScript player;Collider body;TMP_Text text;Camera viewCamera;
        float nextCheck;
        void Awake(){enemy=GetComponent<EnemyScript_space>();body=GetComponent<Collider>();}
        public static bool IsDangerous(float enemyHealth,float playerHealth,float maximum)=>enemyHealth>0&&playerHealth>0&&(enemyHealth>=playerHealth||enemyHealth>=maximum*.2f);
        public static string Label(float enemyHealth,float playerHealth)=>enemyHealth>=playerHealth?"접촉 시\n사망":"충돌\n-"+Mathf.CeilToInt(enemyHealth)+" HP";
        void LateUpdate()
        {
            if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.1f;
            if(player==null)player=FindFirstObjectByType<PlayerScript>();if(viewCamera==null)viewCamera=Camera.main;
            bool show=TimeManager.isGameRunning&&enemy!=null&&player!=null&&IsDangerous(enemy.CurrentHealth,player.currentHealth,player.MaxHealth);
            if(show)
            {
                Vector3 delta=transform.position-player.transform.position;
                float ahead=Vector3.Dot(delta,Vector3.ProjectOnPlane(player.transform.forward,Vector3.up).normalized);
                show=ahead>=-.5f&&ahead<=Mathf.Max(12,player.ForwardMoveSpeed*2.5f)&&Mathf.Abs(delta.y)<2.5f&&delta.sqrMagnitude<900;
            }
            if(!show){if(text!=null)text.gameObject.SetActive(false);return;}
            if(text==null)
            {
                var prefab=Resources.Load<TMP_Text>(ResourcePath);if(prefab==null)return;
                text=Instantiate(prefab,transform);var scale=transform.lossyScale;
                text.transform.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(scale.x)),1/Mathf.Max(.001f,Mathf.Abs(scale.y)),1/Mathf.Max(.001f,Mathf.Abs(scale.z)));
            }
            text.gameObject.SetActive(true);text.text=Label(enemy.CurrentHealth,player.currentHealth);
            text.color=enemy.CurrentHealth>=player.currentHealth?new Color(1,.25f,.2f):new Color(1,.78f,.25f);
            var skin=enemy.GetComponentInChildren<SkinnedMeshRenderer>();float top=skin!=null?skin.bounds.max.y:body!=null?body.bounds.max.y:transform.position.y+3;
            text.transform.position=new Vector3(transform.position.x,top+.42f,transform.position.z);
            if(viewCamera!=null)text.transform.rotation=viewCamera.transform.rotation;
        }
        void OnDisable(){if(text!=null)text.gameObject.SetActive(false);}
    }
}
