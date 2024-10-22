using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class CarAgent : Agent
{
    public Rigidbody2D carRigidbody;
    public Transform finishLine;
    public Transform[] checkpoints; // Array of checkpoints
    private int currentCheckpointIndex = 0; // Keep track of the current checkpoint
    private Vector2 startPosition;

    // Initialize the agent
    public override void Initialize()
    {
        startPosition = transform.position;
        currentCheckpointIndex = 0; // Reset to the first checkpoint at the start
    }

    // Collect observations (distance to current checkpoint and finish line)
    public override void CollectObservations(VectorSensor sensor)
    {
        // Add the position relative to the current checkpoint
        Transform currentCheckpoint = checkpoints[currentCheckpointIndex];
        sensor.AddObservation(Vector2.Distance(transform.position, currentCheckpoint.position));

        // Add the velocity of the car
        sensor.AddObservation(carRigidbody.velocity);

        // Add the direction to the current checkpoint
        Vector2 directionToCheckpoint = (currentCheckpoint.position - transform.position).normalized;
        sensor.AddObservation(directionToCheckpoint);

        // Optional: Add the direction to the finish line
        Vector2 directionToFinish = (finishLine.position - transform.position).normalized;
        sensor.AddObservation(directionToFinish);
    }

    // Take actions based on neural network output
    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveInput = actions.ContinuousActions[0]; // throttle/brake
        float turnInput = actions.ContinuousActions[1]; // steering

        MoveCar(moveInput, turnInput);
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
        currentCheckpointIndex = 0; // Reset to the first checkpoint
    }

    // Reward logic (penalize for hitting walls or reset after finishing the race)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the agent reached the current checkpoint
        if (collision.CompareTag("Checkpoint"))
        {
            // Verify it's the correct checkpoint
            if (collision.transform == checkpoints[currentCheckpointIndex])
            {
                SetReward(1.0f); // Reward for reaching a checkpoint
                currentCheckpointIndex++; // Move to the next checkpoint
            }

            // If all checkpoints are cleared, reward for reaching the finish
            if (currentCheckpointIndex >= checkpoints.Length)
            {
                SetReward(10.0f); // Larger reward for finishing the race
                EndEpisode();
            }
        }
        // Check if the agent hit the finish line (only after passing all checkpoints)
        else if (collision.CompareTag("Finish") && currentCheckpointIndex >= checkpoints.Length)
        {
            SetReward(10.0f); // Reward for finishing the race
            EndEpisode();
        }
        // Penalty for hitting walls or obstacles
        else if (collision.CompareTag("Wall") || collision.CompareTag("Obstacle"))
        {
            SetReward(-2.0f); // Penalty for hitting a wall
            EndEpisode(); // Reset the agent after collision
        }
    }
}
