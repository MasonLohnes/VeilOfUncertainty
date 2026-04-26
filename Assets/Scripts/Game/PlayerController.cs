// =============================================================================
// PlayerController.cs — Player Visual Representation and Animation
// Manages the player's 3D model, position on the grid, facing direction,
// and movement animation between cells.
// Corresponds to Implementation Step 10 (Integration).
// =============================================================================

using UnityEngine;

namespace VeilOfUncertainty
{
    /// <summary>
    /// PlayerController manages the player's visual representation on the grid.
    /// Handles smooth movement animation between cells and facing direction.
    /// The GameManager controls game logic; this handles the visual/physical side.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotateSpeed = 10f;

        [Header("Visual")]
        [SerializeField] private GameObject playerModel;
        [SerializeField] private ParticleSystem moveParticles;
        [SerializeField] private ParticleSystem damageParticles;
        [SerializeField] private ParticleSystem healParticles;

        private Vector3 targetWorldPosition;
        private Quaternion targetRotation;
        private bool isMoving;

        private void Start()
        {
            targetWorldPosition = transform.position;
            targetRotation = transform.rotation;
        }

        private void Update()
        {
            // Smooth movement to target position
            if (isMoving)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, targetWorldPosition, moveSpeed * Time.deltaTime);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetWorldPosition) < 0.01f)
                {
                    transform.position = targetWorldPosition;
                    isMoving = false;
                }
            }
        }

        /// <summary>
        /// Moves the player to a new grid cell position with smooth animation.
        /// </summary>
        public void MoveTo(Vector3 worldPosition, Direction facing)
        {
            targetWorldPosition = worldPosition + Vector3.up * 0.5f;
            targetRotation = GetRotationForDirection(facing);
            isMoving = true;

            if (moveParticles != null)
                moveParticles.Play();
        }

        /// <summary>
        /// Plays the damage visual effect.
        /// </summary>
        public void PlayDamageEffect()
        {
            if (damageParticles != null)
                damageParticles.Play();
        }

        /// <summary>
        /// Plays the heal visual effect (when collecting a resource).
        /// </summary>
        public void PlayHealEffect()
        {
            if (healParticles != null)
                healParticles.Play();
        }

        /// <summary>
        /// Sets the player position immediately (no animation).
        /// </summary>
        public void SetPosition(Vector3 worldPosition)
        {
            targetWorldPosition = worldPosition + Vector3.up * 0.5f;
            transform.position = targetWorldPosition;
            isMoving = false;
        }

        private Quaternion GetRotationForDirection(Direction direction)
        {
            float angle = direction switch
            {
                Direction.North => 0f,
                Direction.East => 90f,
                Direction.South => 180f,
                Direction.West => 270f,
                _ => 0f
            };
            return Quaternion.Euler(0f, angle, 0f);
        }

        public bool IsMoving => isMoving;
    }
}
