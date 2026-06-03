using UnityEngine;

public class CrashDetect : MonoBehaviour
{
    private Rigidbody rb;
    public float crashSpeed = 10f;
    private float previousSpeed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        previousSpeed = rb.velocity.magnitude;
    }

    private void FixedUpdate()
    {
        if ((previousSpeed - rb.velocity.magnitude) > crashSpeed)
        {
            Debug.LogError($"Crashed at {previousSpeed * 3.6f:F2} km/h");
        }
        previousSpeed = rb.velocity.magnitude;
    }
}
