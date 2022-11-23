using CMF;
using UnityEngine;

namespace _Prototyping.MovementFundamentals
{
    public class AddMomentum : MonoBehaviour
    {
        public AdvancedWalkerController WalkerController;
        public Vector3 Momentum;
        
        private bool _pressed;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
                _pressed = true;
        }

        private void FixedUpdate()
        {
            if (_pressed && WalkerController)
                WalkerController.AddMomentum(Momentum);
            _pressed = false;
        }
    }
}