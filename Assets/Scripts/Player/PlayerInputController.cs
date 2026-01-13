using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent

public class PlayerInputController : MonoBehaviour
{
    [System.Serializable] 
    public class InputBinding
    {
        public string actionName; 
        public KeyCode key;  
        public InputType type;   

        [Tooltip("How many times per second the event fires if held down.")]
        public float sensitivity = 10f; 
        public UnityEvent onTrigger;

        private float nextFireTime;

        public void UpdateBinding()
        {
            if (type == InputType.PressOnce)
            {
                if (Input.GetKeyDown(key))
                {
                    onTrigger.Invoke();
                }
            }
            else if (type == InputType.Continuous)
            {
                if (Input.GetKey(key))
                {
                    if (Time.time >= nextFireTime)
                    {
                        onTrigger.Invoke();
                        nextFireTime = Time.time + (1f / Mathf.Max(0.1f, sensitivity));
                    }
                }
            }
        }
    }

    public enum InputType
    {
        PressOnce,
        Continuous
    }

    public bool keyboardControlActive = true;

    public List<InputBinding> keyBindings;

    void Update()
    {
        if (keyboardControlActive)
        {
            foreach (var binding in keyBindings)
            {
                binding.UpdateBinding();
            }
        }
    }
}