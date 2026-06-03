using UnityEngine;

public class LegsController : MonoBehaviour
{
    public Animator LegsAnimator;
    public float LegsDownAltitude = 300;
    
    private void Start()
    {
        if (LegsAnimator == null)
        {
            LegsAnimator = GetComponent<Animator>();    
        }
    }
    
    private void FixedUpdate()
    {
        bool isDown = transform.position.y < LegsDownAltitude;
        LegsAnimator.SetBool("LegsDown", isDown);
    }
}
