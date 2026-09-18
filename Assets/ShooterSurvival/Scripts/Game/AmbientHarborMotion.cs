using UnityEngine;

public sealed class AmbientHarborMotion : MonoBehaviour
{
    public bool bird;
    public float phase, amplitude=.08f;
    private Vector3 origin;
    private Quaternion rotation;
    private void Start()
    {
        origin=transform.localPosition;rotation=transform.localRotation;
        if(bird)
        {
            var animator=GetComponentInChildren<Animator>();
            if(animator!=null)foreach(var parameter in animator.parameters)if(parameter.name=="Fly"){animator.SetTrigger("Fly");break;}
        }
    }
    private void Update()
    {
        float time=Time.time+phase;
        transform.localPosition=origin+Vector3.up*(Mathf.Sin(time*1.3f)*amplitude);
        if(bird)transform.localPosition+=new Vector3(Mathf.Sin(time*.35f)*2,0,Mathf.Cos(time*.35f)*2);
        else transform.localRotation=rotation*Quaternion.Euler(0,0,Mathf.Sin(time*.9f)*1.2f);
    }
}
