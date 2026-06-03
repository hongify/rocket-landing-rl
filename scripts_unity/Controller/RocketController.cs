using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RocketController : MonoBehaviour
{
    [Header("Flight Settings")]
    public float enginePower = 100f;
    public float gridFinRotationSpeed = 100f;
    public float thrusterPower = 10f;

    private Rigidbody rb;

    [Header("Components")]
    [SerializeField]
    private GameObject engine;
    [SerializeField]
    private GameObject thruster;
    [SerializeField]
    private List<GameObject> gridFins;

    private Vector3[] gridFinDefaultRotations;
    private Vector3[] gridFinPositiveLimits;
    private Vector3[] gridFinNegativeLimits;

    [Header("UI & Particles")]
    public TMP_Text displayText;
    [SerializeField]
    private ParticleSystem westParticle;
    [SerializeField]
    private ParticleSystem eastParticle;
    [SerializeField]
    private ParticleSystem northParticle;
    [SerializeField]
    private ParticleSystem southParticle;
    [SerializeField]
    private ParticleSystem engineParticle;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        gridFinDefaultRotations = new Vector3[gridFins.Count];
        gridFinPositiveLimits = new Vector3[gridFins.Count];
        gridFinNegativeLimits = new Vector3[gridFins.Count];

        gridFinDefaultRotations[0] = new Vector3(-135, -90, 0); 
        gridFinPositiveLimits[0] = new Vector3(-142, -129, 26); 
        gridFinNegativeLimits[0] = new Vector3(-142, -50, -26);

        gridFinDefaultRotations[1] = new Vector3(-45, -90, 0); 
        gridFinPositiveLimits[1] = new Vector3(-37, -129, 26);
        gridFinNegativeLimits[1] = new Vector3(-37, -50, -26);

        gridFinDefaultRotations[2] = new Vector3(-45, 270, 0); 
        gridFinPositiveLimits[2] = new Vector3(-37, 231, 26);
        gridFinNegativeLimits[2] = new Vector3(-37, 310, -26);

        gridFinDefaultRotations[3] = new Vector3(-135, -90, 0); 
        gridFinPositiveLimits[3] = new Vector3(-142, -129, 26);
        gridFinNegativeLimits[3] = new Vector3(-142, -50, -26);
    }

    public void ApplyEngineForce(float thrust, Vector2 direction)
    {
        float clampedThrust = Mathf.Clamp(thrust, 0, 1) * enginePower;
        Quaternion targetRotation = Quaternion.Euler(direction.x * 15f, direction.y * 15f, 0);

        engine.transform.localRotation = Quaternion.Lerp(
            engine.transform.localRotation,
            targetRotation,                 
            Time.deltaTime * 5f 
        );

        Vector3 engineForceDirection = engine.transform.forward;
        Vector3 engineForce = engineForceDirection * clampedThrust;
        Vector3 enginePosition = engine.transform.position;

        SetEmissionRate(clampedThrust);
        rb.AddForceAtPosition(engineForce, enginePosition);
    }

    public void SetEmissionRate(float rate)
    {
        if (engineParticle == null) return;
        var emission = engineParticle.emission;
        emission.rateOverTime = rate / 2500f;
    }

    public void ApplyThrusterForce(int direction)
    {
        Vector3 thrusterDirection = Vector3.zero;
        DisableAllParticles();

        switch (direction)
        {
            case 0: thrusterDirection = Vector3.zero; break;      
            case 1: // West
                thrusterDirection = Vector3.left;
                ActivateParticle(westParticle);
                break;
            case 2: // East
                thrusterDirection = Vector3.right;
                ActivateParticle(eastParticle);
                break;
            case 3: // North
                thrusterDirection = Vector3.forward;
                ActivateParticle(northParticle);
                break;
            case 4: // South
                thrusterDirection = Vector3.back;
                ActivateParticle(southParticle);
                break;
        }

        if (thrusterDirection != Vector3.zero)
        {
            Vector3 thrusterPosition = thruster.transform.position;
            Vector3 thrusterForce = thrusterDirection * thrusterPower;
            rb.AddForceAtPosition(thrusterForce, thrusterPosition);
        }
    }

    private void DisableAllParticles()
    {
        SetEmission(westParticle, false);
        SetEmission(eastParticle, false);
        SetEmission(northParticle, false);
        SetEmission(southParticle, false);
    }

    private void ActivateParticle(ParticleSystem particle)
    {
        SetEmission(particle, true);
    }

    private void SetEmission(ParticleSystem particle, bool enable)
    {
        if (particle == null) return;
        var emission = particle.emission;
        emission.enabled = enable;
    }

    public void ControlGridFins(float[] inputs)
    {
        for (int i = 0; i < gridFins.Count; i++)
        {
            float input = Mathf.Clamp(inputs[i], -1, 1);
            Vector3 targetRotation = Vector3.zero;
            
            if (input < 0)
            {
                targetRotation = Vector3.Lerp(gridFinDefaultRotations[i], gridFinNegativeLimits[i], Mathf.Abs(input));
            }
            else if (input > 0) 
            {
                targetRotation = Vector3.Lerp(gridFinDefaultRotations[i], gridFinPositiveLimits[i], input);
            }
            else 
            {
                targetRotation = gridFinDefaultRotations[i];
            }

            gridFins[i].transform.localRotation = Quaternion.RotateTowards(
                gridFins[i].transform.localRotation,
                Quaternion.Euler(targetRotation),
                gridFinRotationSpeed * Time.deltaTime
            );
        }
    }

    public void displayTexts(float engineThrust, Vector2 engineDirection, int thrusterDirection, float[] gridFinInputs)
    {
        if (displayText == null) return;

        displayText.text = $"Velocity: {(int)(rb.velocity.magnitude * 3.6f):D3} km/h\n";
        displayText.text += $"Altitude: {(int)transform.position.y:D4} m\n\n";
        displayText.text += $"Engine Force: {(int)(engineThrust * 100)}%\n";
        displayText.text += $"Engine Direction: {engineDirection}\n";
        displayText.text += $"Cold Gas Thruster: {thrusterDirection}\n\n";

        for (int i = 0; i < gridFins.Count; i++)
        {
            displayText.text += $"GridFin {i}: {gridFinInputs[i]:F2}\n";
        }
    }
}