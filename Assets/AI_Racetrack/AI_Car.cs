using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class CarAgent : Agent
{
    public Rigidbody2D carRigidbody;
    public Transform finishLine;
    private Vector2 startPosition;

    private float maxEpisodeLength = 500f; // Example time limit
    private float currentEpisodeTime = 0f;

    // Initialize the agent
    public override void Initialize()
    {
        startPosition = transform.position;
    }

    // Collect observations (distance from the track and other cars)
    public override void CollectObservations(VectorSensor sensor)
    {
        // Add the position relative to the finish line (distance and direction)
        Vector2 directionToFinish = (finishLine.position - transform.position).normalized;
        sensor.AddObservation(directionToFinish); // Direction to the finish line
        sensor.AddObservation(Vector2.Distance(transform.position, finishLine.position));

        // Add the velocity of the car
        sensor.AddObservation(carRigidbody.velocity);

        // Add the car's orientation (angle in relation to track direction)
        sensor.AddObservation(transform.up);

        // Optionally: Add more observations, such as distances to the nearest walls, other cars, etc.
    }


    // Take actions based on neural network output
    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveInput = actions.ContinuousActions[0]; // throttle/brake
        float turnInput = actions.ContinuousActions[1]; // steering

        MoveCar(moveInput, turnInput);

        // Reward the agent for moving closer to the finish line
        float distanceToFinish = Vector2.Distance(transform.position, finishLine.position);
        SetReward(1.0f / distanceToFinish); // Higher reward for closer proximity

        currentEpisodeTime += Time.deltaTime;
        if (currentEpisodeTime > maxEpisodeLength)
        {
            SetReward(-1.0f); // Penalty for not finishing in time
            EndEpisode();
        }
        // Optionally: Penalize for straying from the track
        // if (OffTrack()) SetReward(-0.01f);
    }


    private void MoveCar(float moveInput, float turnInput)
    {
        // Apply movement logic (simple physics-based movement)
        Vector2 move = transform.up * moveInput * 10f;
        carRigidbody.AddForce(move);

        float turn = -turnInput * 200f;
        carRigidbody.AddTorque(turn);
    }

    // Reset the agent (used during training)
    public override void OnEpisodeBegin()
    {
        carRigidbody.velocity = Vector2.zero;
        carRigidbody.angularVelocity = 0;
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        currentEpisodeTime = 0f;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Vertical"); // Throttle/brake
        continuousActions[1] = Input.GetAxisRaw("Horizontal"); // Steering
    }


    // Reward logic (based on distance to the finish line, or penalize for hitting walls)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Finish"))
        {
            SetReward(10.0f); // Reward for finishing the race
            EndEpisode();
        }
        else if (collision.CompareTag("Wall"))
        {
            SetReward(-2.0f); // Penalty for hitting the wall
            EndEpisode();
        }
    }
}
