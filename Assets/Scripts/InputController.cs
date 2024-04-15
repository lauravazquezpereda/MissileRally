using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    private CarController car;

    private void Start()
    {
        car = GetComponent<Player>().car.GetComponent<CarController>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        //actualizo los valores de car controller
        var input = context.ReadValue<Vector2>();
        car.InputAcceleration = input.y;
        car.InputSteering = input.x;
    }

    public void OnBrake(InputAction.CallbackContext context)
    {
        //actualizo los valores del freno
        var input = context.ReadValue<float>();
        car.InputBrake = input;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
    }
}