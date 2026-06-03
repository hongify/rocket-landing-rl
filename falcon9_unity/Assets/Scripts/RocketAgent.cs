using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class RocketAgent : Agent
{
    [SerializeField]
    private RocketController rocketController;
    public Material landingPadMaterial; 
    public Color successColor = Color.green; 
    public Color failureColor = Color.red; 

    [SerializeField]
    private Transform targetLandingZone; 

    private Rigidbody rb;
    private float previousSpeed;
    
    [SerializeField]
    private float crashSpeed = 15f;
    private bool notPassed;
    private bool cutEngine;
    private float previousVerticalSpeed;
    
    [SerializeField]
    private GameObject landedText;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        notPassed = true;
        cutEngine = false;
        
        if (landedText != null)
        {
            landedText.SetActive(false);
        }
        
        previousVerticalSpeed = Mathf.Abs(rb.velocity.y);
       
        transform.position = targetLandingZone.position + new Vector3(0, 5000, 0);
        rocketController.ApplyEngineForce(0, new Vector2(0, 0));

        transform.rotation = Quaternion.Euler(0, 0, 0);
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        previousSpeed = 0;

        targetLandingZone.position = new Vector3(
            targetLandingZone.position.x,
            targetLandingZone.position.y,
            targetLandingZone.position.z
        );
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(rb.velocity);       
        sensor.AddObservation(rb.angularVelocity); 
        sensor.AddObservation(transform.rotation); 
        
        sensor.AddObservation(transform.localPosition); 
        sensor.AddObservation(targetLandingZone.localPosition);
        sensor.AddObservation(transform.localPosition - targetLandingZone.localPosition); 
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (cutEngine == false)
        {
            float engineThrust = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f); 
            float engineDirectionX = actions.ContinuousActions[1]; 
            float engineDirectionY = actions.ContinuousActions[2]; 

            float[] gridFinInputs = new float[4];
            for (int i = 0; i < 4; i++)
            {
                gridFinInputs[i] = actions.ContinuousActions[3 + i];
            }

            int thrusterDirection = actions.DiscreteActions[0]; 

            rocketController.ApplyEngineForce(engineThrust, new Vector2(engineDirectionX, engineDirectionY));
            rocketController.ApplyThrusterForce(thrusterDirection);
            rocketController.ControlGridFins(gridFinInputs);
            rocketController.displayTexts(engineThrust, new Vector2(engineDirectionX, engineDirectionY), thrusterDirection, gridFinInputs);

            CalculateReward();
            CheckTerminationConditions();
        }
        else 
        {
            rocketController.ApplyEngineForce(0, Vector2.zero);
            rocketController.ApplyThrusterForce(0);
            rocketController.ControlGridFins(new float[4]);
            rocketController.displayTexts(0, Vector2.zero, 0, new float[4]);
        }
    }

    public void EndEpisode(float reward)
    {
        AddReward(reward);
        StartCoroutine(WaitCoroutine());
        if (reward < -0.001f)
        {
            ChangeColorRed();
        }
        EndEpisode();
    }

    IEnumerator WaitCoroutine()
    {
        yield return new WaitForSeconds(0.1f); 
    }

    private void CalculateReward()
    {
        float horizontalDistanceToTarget = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(targetLandingZone.position.x, targetLandingZone.position.z)
        );
        float altitude = transform.position.y;
        float verticalSpeed = Mathf.Abs(rb.velocity.y);
        float angularVelocityMagnitude = rb.angularVelocity.magnitude;
        Vector3 rotation = transform.eulerAngles;

        if (altitude > 1600)
        {
            AddReward(-0.05f * Time.fixedDeltaTime);

            float rotationX = Mathf.DeltaAngle(rotation.x, 0);
            float rotationZ = Mathf.DeltaAngle(rotation.z, 0);
            float rotationSum = Mathf.Abs(rotationX) + Mathf.Abs(rotationZ);

            if (rotationSum <= 10.0f)
            {
                AddReward(0.04f * Time.deltaTime);
            }
            else 
            {
                AddReward(-0.02f * Mathf.Clamp(rotationSum / 45.0f, 0, 1) * Time.deltaTime);
                if (rotationSum > 45.0f) 
                {
                    EndEpisode(-1f);
                }
            }

            if (horizontalDistanceToTarget <= 25.0f)
            {
                AddReward(0.04f * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, targetLandingZone.position) <= 200.0f)
            {
                AddReward(0.04f * Time.deltaTime);
            }
            else
            {
                float distancePenalty = Mathf.Clamp01((horizontalDistanceToTarget - 25.0f) / 45.0f);
                AddReward(-0.02f * distancePenalty * Time.deltaTime);
                if (horizontalDistanceToTarget > 70.0f)
                {
                    EndEpisode(-1f);
                }
            }

            float angularVelocityPenalty = Mathf.Clamp01(rb.angularVelocity.magnitude / 3.0f);
            if (rb.angularVelocity.magnitude <= 1.5f) 
            {
                AddReward(0.04f * Time.deltaTime); 
            }
            else
            {
                AddReward(-0.02f * angularVelocityPenalty * Time.deltaTime);
                if (rb.angularVelocity.magnitude > 3f)
                {
                    EndEpisode(-1f);
                }
            }
        }
        else
        {
            if (notPassed)
            {
                AddReward(10f);
                notPassed = false;
            }
            
            AddReward(-0.75f * Time.fixedDeltaTime);
            float rotationPenalty = Mathf.Abs(rotation.x) + Mathf.Abs(rotation.z); 
                   
            if (horizontalDistanceToTarget < 250f)
            {
                AddReward(0.1f * Mathf.Clamp01(1 - horizontalDistanceToTarget / 250f) * Time.deltaTime);
                if (horizontalDistanceToTarget < 150f)
                {
                    AddReward(0.1f * Mathf.Clamp01(1 - horizontalDistanceToTarget / 250f) * Time.deltaTime);
                }
            }

            float maxAllowedSpeed = Mathf.Lerp(10f, 290f, altitude / 1600f);
            if (verticalSpeed > maxAllowedSpeed)
            {
                AddReward(-1f * (verticalSpeed - maxAllowedSpeed) / maxAllowedSpeed * 10 * Time.deltaTime);
            }

            float currentVerticalSpeed = Mathf.Abs(rb.velocity.y); 
            float speedReduction = previousVerticalSpeed - currentVerticalSpeed; 
            if (speedReduction > 0)
            {
                AddReward(speedReduction * 0.15f); 
            }
            previousVerticalSpeed = currentVerticalSpeed;

            if (rotationPenalty > 30.0f) 
            {
                AddReward(-0.1f * Time.deltaTime);
            }

            if (horizontalDistanceToTarget < 80f && altitude < 150 && rb.velocity.magnitude < 15)
            {
                AddReward(0.25f * Time.deltaTime);
            }

            if (horizontalDistanceToTarget < 75f &&
                altitude <= 4f &&
                verticalSpeed < 1f &&
                angularVelocityMagnitude < 1f &&
                rotationPenalty < 10.0f)
            {
                Debug.Log("Landed Successfully!");
                cutEngine = true;
                if (landedText != null)
                {
                    landedText.SetActive(true);
                }
                EndEpisode(50.0f);
            }
        }
    }

    private void CheckTerminationConditions()
    {
        float horizontalDistanceToTarget = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(targetLandingZone.position.x, targetLandingZone.position.z)
        );
        float altitude = transform.position.y;
        float maxAllowedHorizontalDistance = 300f;

        if (altitude <= 5000f && altitude > 1000f) maxAllowedHorizontalDistance = 200f;
        else if (altitude <= 1000f && altitude > 500f) maxAllowedHorizontalDistance = 150f;
        else if (altitude <= 500f) maxAllowedHorizontalDistance = 100f;

        if (horizontalDistanceToTarget > maxAllowedHorizontalDistance)
        {
            EndEpisode(-4.0f);
            return;
        }

        if (altitude < -10f)
        {
            EndEpisode(-5.0f);
            return;
        }

        if ((previousSpeed - rb.velocity.magnitude) > crashSpeed)
        {
            EndEpisode(-5.0f);
            return;
        }
        previousSpeed = rb.velocity.magnitude;

        if (rb.velocity.y >= 10)
        {
            EndEpisode(-5.0f);
        }

        if (altitude < 10f && rb.velocity.magnitude < 0.1f)
        {
            EndEpisode(-5.0f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        var discreteActions = actionsOut.DiscreteActions;

        continuousActions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
        continuousActions[1] = Input.GetAxis("Vertical");         
        continuousActions[2] = Input.GetAxis("Horizontal");       

        for (int i = 0; i < 4; i++)
        {
            continuousActions[3 + i] = Input.GetKey(KeyCode.T) ? 1 : (Input.GetKey(KeyCode.Y) ? -1 : 0);
        }

        if (Input.GetKey(KeyCode.LeftArrow)) discreteActions[0] = 1;
        else if (Input.GetKey(KeyCode.RightArrow)) discreteActions[0] = 2;
        else if (Input.GetKey(KeyCode.UpArrow)) discreteActions[0] = 3;   
        else if (Input.GetKey(KeyCode.DownArrow)) discreteActions[0] = 4;  
        else discreteActions[0] = 0; 
    }

    public void ChangeColorBlue()
    {
        StartCoroutine(ChangeColorRoutine(successColor));
    }

    public void ChangeColorRed()
    {
        StartCoroutine(ChangeColorRoutine(failureColor));
    }

    private IEnumerator ChangeColorRoutine(Color targetColor)
    {
        if (landingPadMaterial != null)
        {
            // landingPadMaterial.color = targetColor;
            yield return new WaitForSeconds(2);
        }
    }
}
