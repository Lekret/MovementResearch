//#define MB_DEBUG

using UnityEngine;
using MenteBacata.ScivoloCharacterController.Internal;
using static MenteBacata.ScivoloCharacterController.Internal.OverlapResolver;

namespace MenteBacata.ScivoloCharacterController
{
    public class CharacterCapsule : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Vertical offset from the game object position to the bottom of the capsule.")]
        private float verticalOffset = 0f;

        [SerializeField]
        [Tooltip("Height of the capsule.")]
        private float height = 2f;

        [SerializeField]
        [Tooltip("Raidus of the capsule.")]
        [Min(0f)]
        private float radius = 0.5f;

        [Min(0f)]
        [Tooltip("Small distance from the surface of the capsule used as a safety margin to avoid that the capsule is directly in contact with other colliders.")]
        public float contactOffset = 0.01f;

        [Tooltip("Overlaps with colliders in layers excluded from this mask will be ignored if the attempt to resolve all overlaps fails.")]
        public LayerMask prioritizedOverlap = Physics.AllLayers;

        private LayerMask collisionMask;


        /// <summary>
        /// Vertical offset from the game object position to the bottom of the capsule.
        /// </summary>
        public float VerticalOffset
        {
            get => verticalOffset;
            set 
            { 
                verticalOffset = value; 
                SetColliderProperties(); 
            }
        }

        /// <summary>
        /// Height of the capsule.
        /// </summary>
        public float Height
        {
            get => height;
            set 
            {
                height = value;
                ValidateHeight();
                SetColliderProperties(); 
            }
        }

        /// <summary>
        /// Radius of the capsule.
        /// </summary>
        public float Radius
        {
            get => radius;
            set 
            {
                radius = value;
                ValidateHeight();
                SetColliderProperties(); 
            }
        }

        /// <summary>
        /// Capsule up direction.
        /// </summary>
        public Vector3 UpDirection
        {
            get => transform.up;
            set => transform.up = value;
        }

        /// <summary>
        /// Rotation of the capsule.
        /// </summary>
        public Quaternion Rotation
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }

        /// <summary>
        /// World space center of the capsule.
        /// </summary>
        public Vector3 Center => transform.position + transform.TransformVector(LocalCenter);

        /// <summary>
        /// World space center of the capsule lower hemisphere.
        /// </summary>
        public Vector3 LowerHemisphereCenter => transform.position + ToLowerHemisphereCenter;

        /// <summary>
        /// World space center of the capsule upper hemisphere.
        /// </summary>
        public Vector3 UpperHemisphereCenter => transform.position + ToUpperHemisphereCenter;

        /// <summary>
        /// World space vector from game object position to the center of the capsule lower hemisphere.
        /// </summary>
        internal Vector3 ToLowerHemisphereCenter => transform.TransformVector(0f, verticalOffset + radius, 0f);

        /// <summary>
        /// World space vector from game object position to the center of the capsule upper hemisphere.
        /// </summary>
        internal Vector3 ToUpperHemisphereCenter => transform.TransformVector(0f, verticalOffset + height - radius, 0f);

        internal CapsuleCollider Collider { get; private set; }

        internal Rigidbody Rigidbody { get; private set; }

        internal LayerMask CollisionMask => collisionMask;

        private Vector3 LocalCenter => new Vector3(0f, verticalOffset + 0.5f * height, 0f);


        private void Awake()
        {
            DoPreliminaryCheck();
            InstantiateComponents();

            collisionMask = gameObject.GetCollisionMask();
        }

        private void Update()
        {
            TryResolveOverlap();
        }

        /// <summary>
        /// Tries to resolve overlaps with every colliders it is supposed to collide with. If the first attempt fails, it tries again 
        /// considering only the high priority colliders.
        /// </summary>
        /// <returns>Returns true if it managed to resolve all overlaps, false otherwise.</returns>
        public bool TryResolveOverlap()
        {
            Vector3 initialPosition = transform.position;

            if (TryResolveCapsuleOverlap(this, initialPosition, contactOffset, collisionMask, out Vector3 newPosition))
            {
                transform.position = newPosition;
                return true;
            }

            LayerMask prioritizedCollisionMask = collisionMask & prioritizedOverlap;

            if (prioritizedCollisionMask == collisionMask)
            {
                transform.position = newPosition;
                return false;
            }

            TryResolveCapsuleOverlap(this, initialPosition, contactOffset, prioritizedCollisionMask, out newPosition);

            transform.position = newPosition;
            return false;
        }

        private void DoPreliminaryCheck()
        {
            if (!Mathf.Approximately(transform.lossyScale.x, 1f) ||
                !Mathf.Approximately(transform.lossyScale.y, 1f) ||
                !Mathf.Approximately(transform.lossyScale.z, 1f))
            {
                Debug.LogWarning($"{nameof(CharacterCapsule)}: Object scale is not (1, 1, 1).");
            }
            
            foreach (var col in gameObject.GetComponentsInChildren<Collider>(true))
            {
                if (col != Collider && !col.isTrigger && !Physics.GetIgnoreLayerCollision(gameObject.layer, col.gameObject.layer))
                {
                    Debug.LogWarning($"{nameof(CharacterCapsule)}: Found other colliders on this gameobject or in its childrens.");
                    break;
                }
            }
        }

        private void InstantiateComponents()
        {
            Collider = gameObject.AddComponent<CapsuleCollider>();
            SetColliderProperties();

            Rigidbody = gameObject.AddComponent<Rigidbody>();
            Rigidbody.isKinematic = true;
        }

        private void ValidateHeight()
        {
            if (height >= 2f * radius)
                return;

            height = 2f * radius;
        }

        private void SetColliderProperties()
        {
            if (Collider is null)
                return;

            Collider.center = LocalCenter;
            Collider.height = height;
            Collider.radius = radius;
            Collider.direction = 1; // Y-Axis
        }

        private void OnValidate()
        {
            ValidateHeight();

            if (Collider != null)
                SetColliderProperties();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GizmosUtility.defaultColliderColor;
            GizmosUtility.DrawWireCapsule(LowerHemisphereCenter, UpperHemisphereCenter, radius);
        }
    }
}
