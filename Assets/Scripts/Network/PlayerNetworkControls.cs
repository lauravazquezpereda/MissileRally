using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkControls : NetworkBehaviour
{
    public GameObject car;
    public CarController carController;
    float aceleracion;
    float direccion;

    // Start is called before the first frame update
    void Start()
    {
        // Se asigna el controlador para poder manejar el coche
        carController = car.GetComponent<CarController>();
        UI_HUD.Instance.inicioCarrera = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!IsOwner) return; // Solo puede moverse su propio objeto player no el de lo dem�s jugadores

        carController.IsOwner = true;
        // CONTROLES DEL COCHE //
        aceleracion = Input.GetAxis("Vertical"); // Controles asignados a las teclas WS
        direccion = Input.GetAxis("Horizontal"); // Controles asignados a las teclas AD

        ProcessMovementServerRpc(aceleracion, direccion, carController.ID);

    }

    [ServerRpc]
    private void ProcessMovementServerRpc(float aceleracion, float direccion, int idJugador)
    {
        // Se recorren todos los jugadores que están en la carrera, hasta que se encuentra aquel cuyo id coincide con el del que ha enviado el mensaje
        foreach (PlayerNetwork jugador in RaceController.instance._players)
        {
            if (jugador.ID == idJugador)
            {
                // Se procesan la aceleracion y la direccion
                CarController cocheJugador = jugador.GetComponentInChildren<CarController>();
                cocheJugador.InputAcceleration = aceleracion;
                cocheJugador.InputSteering = direccion * 0.5f; // Para disminuir la brusquedad al cambiar de direccion
                Debug.Log("El coche " + idJugador + " debe tener una aceleracion de " + aceleracion + " y una direccion de " + direccion);
            }
        }
        // Se sincroniza con todos los clientes
        UpdatePositionClientRpc(aceleracion, direccion, idJugador);

    }

    [ClientRpc]
    private void UpdatePositionClientRpc(float aceleracion, float direccion, int idJugador)
    {
        // Se recorren todos los jugadores que están en la carrera, hasta que se encuentra aquel cuyo id coincide con el del que ha enviado el mensaje
        foreach (PlayerNetwork jugador in RaceController.instance._players)
        {
            if (jugador.ID == idJugador)
            {
                // Se procesan la aceleracion y la direccion
                CarController cocheJugador = jugador.GetComponentInChildren<CarController>();
                cocheJugador.InputAcceleration = aceleracion;
                cocheJugador.InputSteering = direccion * 0.5f; // Para disminuir la brusquedad al cambiar de direccion
                // ACTUALIZACIÓN DEL HUD //
                float velocidadActual = cocheJugador._currentSpeed;
                UI_HUD.Instance.ModificarVelocimetro(velocidadActual);
                Debug.Log("El coche " + idJugador + " debe tener una aceleracion de " + aceleracion + " y una direccion de " + direccion);
            }
        }
    }

}


