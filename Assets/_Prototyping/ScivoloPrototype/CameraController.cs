using System;
using UnityEngine;

namespace _Prototyping.ScivoloPrototype
{
    public class CameraController : MonoBehaviour
    {
        public float verticalOffsetFromPlayer;
        public float distanceFromPlayer;
        public Transform playerTransform;
        public Transform cameraTransform;
        public float sensitivity;
        
        private void LateUpdate()
        {
            var pitch = Input.GetAxis("Mouse Y");
            var yaw = Input.GetAxis("Mouse X");

            var playerEuler = playerTransform.eulerAngles;
            playerEuler.y += yaw * sensitivity;
            playerTransform.eulerAngles = playerEuler;

            var cameraEuler = cameraTransform.eulerAngles;
            cameraEuler.x -= pitch * sensitivity;
            cameraEuler.x = ClampAngle(cameraEuler.x, -80, 80);
            cameraEuler.y = playerEuler.y;
            cameraTransform.eulerAngles = cameraEuler;

            var cameraPosition = playerTransform.position + Vector3.up * verticalOffsetFromPlayer;
            cameraPosition -= cameraTransform.forward * distanceFromPlayer;
            cameraTransform.position = cameraPosition;
        }
        
        private static float ClampAngle(float ang, float min, float max)
        {
            var nMin = Mathf.DeltaAngle(ang, min);
            var nMax = Mathf.DeltaAngle(ang, max);

            if (nMin <= 0 && nMax >= 0)
                return ang;
            return Mathf.Abs(nMin) < Mathf.Abs(nMax) ? min : max;
        }
    }
}