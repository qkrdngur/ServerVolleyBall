using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "SO/InputReader")]
public class InputReader : ScriptableObject, Controls.IPlayerActions
{
    public Vector2 MovePosition { get; private set; }
    public event Action<bool> OnJumpEvent;

    private Controls _controls;

    private void OnEnable()
    {
        if(_controls == null)
        {
            _controls = new Controls();
            _controls.Player.SetCallbacks(this);
        }

        _controls.Player.Enable();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnJumpEvent?.Invoke(true);
        }
        else if (context.canceled)
        {
            OnJumpEvent?.Invoke(false);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MovePosition = context.ReadValue<Vector2>();
    }
}
