using MenteBacata.ScivoloCharacterControllerDemo;
using UnityEngine;

namespace _Prototyping.ScivoloPrototype
{
    public class UpdateOrder : MonoBehaviour
    {
        private void Update()
        {
            foreach (var plat in FindObjectsOfType<MovingPlatform>())
            {
                plat.CustomUpdate();
            }

            foreach (var collector in FindObjectsOfType<PlatformCollector>())
            {
                collector.CustomUpdate();
            }
            
            FindObjectOfType<ScivoloKinematicMovement>().CustomUpdate();
            FindObjectOfType<ScivoloKinematicMovement>().CustomMoveToPlatformUpdate();

            Physics.SyncTransforms();
        }
    }
}