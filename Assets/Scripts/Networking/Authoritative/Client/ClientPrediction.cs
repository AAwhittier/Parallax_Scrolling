using System.Collections.Generic;
using UnityEngine;

namespace SimpleNetworking.Authoritative
{
    /// <summary>
    /// Predicted input with result state for reconciliation
    /// </summary>
    public class PredictedInput
    {
        public int sequenceId;
        public PlayerInput input;
        public Vector2 resultPosition;
        public Vector2 resultVelocity;
        public float timestamp;

        public PredictedInput(int seq, PlayerInput inp, Vector2 pos, Vector2 vel)
        {
            sequenceId = seq;
            input = inp;
            resultPosition = pos;
            resultVelocity = vel;
            timestamp = Time.time;
        }
    }

    /// <summary>
    /// Client-side prediction and reconciliation for local player.
    /// Predicts movement immediately, then corrects when server snapshot arrives.
    /// </summary>
    public class ClientPrediction : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Maximum prediction error before forcing correction")]
        public float maxPredictionError = 1f;

        [Tooltip("Smooth correction speed")]
        public float correctionSpeed = 10f;

        [Header("Status")]
        [SerializeField]
        private int currentSequenceId = 0;

        [SerializeField]
        private int lastAcknowledgedInput = 0;

        [SerializeField]
        private int pendingInputsCount = 0;

        [SerializeField]
        private float currentPredictionError = 0f;

        // Local player state
        private Vector2 predictedPosition;
        private Vector2 predictedVelocity;
        private int facingDirection = 1;

        // Pending inputs (not yet acknowledged by server)
        private List<PredictedInput> pendingInputs = new List<PredictedInput>();

        // Physics constants (must match server)
        private const float MOVE_SPEED = 8f;
        private const float JUMP_FORCE = 10f;
        private const float GRAVITY = 20f;
        private const float GROUND_Y = 0f;

        // Reference to game client
        private AuthoritativeGameClient gameClient;

        void Awake()
        {
            gameClient = GetComponent<AuthoritativeGameClient>();
        }

        /// <summary>
        /// Process local input with prediction
        /// </summary>
        public void ProcessLocalInput(PlayerInput input)
        {
            input.sequenceId = currentSequenceId++;

            // Apply input locally (predict)
            ApplyInput(input);

            // Store for reconciliation
            pendingInputs.Add(new PredictedInput(
                input.sequenceId,
                input,
                predictedPosition,
                predictedVelocity
            ));

            // Send to server
            if (gameClient != null)
            {
                gameClient.SendInput(input);
            }

            pendingInputsCount = pendingInputs.Count;

            // Limit pending inputs (prevent memory leak if server stops responding)
            if (pendingInputs.Count > 120) // 2 seconds at 60fps
            {
                pendingInputs.RemoveAt(0);
            }
        }

        /// <summary>
        /// Apply input to predicted state (mimics server logic)
        /// </summary>
        private void ApplyInput(PlayerInput input)
        {
            float deltaTime = Time.fixedDeltaTime;
            bool isGrounded = predictedPosition.y <= GROUND_Y;

            // Movement
            Vector2 moveInput = input.GetMoveVector();
            predictedVelocity.x = moveInput.x * MOVE_SPEED;

            // Update facing
            if (moveInput.x != 0)
            {
                facingDirection = moveInput.x > 0 ? 1 : -1;
            }

            // Jump
            if (input.jump && isGrounded)
            {
                predictedVelocity.y = JUMP_FORCE;
            }

            // Gravity
            if (!isGrounded)
            {
                predictedVelocity.y -= GRAVITY * deltaTime;
            }

            // Apply velocity
            predictedPosition += predictedVelocity * deltaTime;

            // Ground collision
            if (predictedPosition.y <= GROUND_Y)
            {
                predictedPosition.y = GROUND_Y;
                predictedVelocity.y = 0;
            }

            // Friction
            predictedVelocity.x *= 0.9f;

            // Update visual position immediately
            transform.position = new Vector3(predictedPosition.x, predictedPosition.y, transform.position.z);
        }

        /// <summary>
        /// Reconcile with server state
        /// </summary>
        public void OnServerSnapshot(PlayerSnapshot serverState)
        {
            lastAcknowledgedInput = serverState.lastProcessedInput;

            // Remove acknowledged inputs
            pendingInputs.RemoveAll(p => p.sequenceId <= lastAcknowledgedInput);
            pendingInputsCount = pendingInputs.Count;

            // Server position
            Vector2 serverPosition = serverState.GetPosition();
            Vector2 serverVelocity = serverState.GetVelocity();

            // Calculate prediction error
            currentPredictionError = Vector2.Distance(serverPosition, predictedPosition);

            // If error is small, smoothly correct
            if (currentPredictionError < maxPredictionError)
            {
                // Smooth correction
                predictedPosition = Vector2.Lerp(predictedPosition, serverPosition, correctionSpeed * Time.deltaTime);
                predictedVelocity = Vector2.Lerp(predictedVelocity, serverVelocity, correctionSpeed * Time.deltaTime);
            }
            else
            {
                // Large error - snap to server position and replay inputs
                Debug.Log($"[Prediction] Large error ({currentPredictionError:F2}), reconciling...");

                predictedPosition = serverPosition;
                predictedVelocity = serverVelocity;

                // Replay pending inputs
                foreach (var predicted in pendingInputs)
                {
                    ApplyInput(predicted.input);
                }

                Debug.Log($"[Prediction] Replayed {pendingInputs.Count} inputs");
            }
        }

        /// <summary>
        /// Initialize position from server
        /// </summary>
        public void InitializeFromServer(Vector2 position, Vector2 velocity)
        {
            predictedPosition = position;
            predictedVelocity = velocity;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            pendingInputs.Clear();
        }

        /// <summary>
        /// Get predicted position
        /// </summary>
        public Vector2 GetPredictedPosition()
        {
            return predictedPosition;
        }

        /// <summary>
        /// Get predicted velocity
        /// </summary>
        public Vector2 GetPredictedVelocity()
        {
            return predictedVelocity;
        }
    }
}
