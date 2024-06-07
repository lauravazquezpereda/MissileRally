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
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!IsOwner) return; // Solo puede moverse su propio objeto player no el de lo dem�s jugadores

        // CONTROLES DEL COCHE //
        aceleracion = Input.GetAxis("Vertical"); // Controles asignados a las teclas WS
        direccion = Input.GetAxis("Horizontal"); // Controles asignados a las teclas AD

        // Asignaci�n de la entrada a los par�metros de los coches
        /*
        carController.InputAcceleration = aceleracion;
        carController.InputSteering = direccion * 0.5f; // Para disminuir la brusquedad al cambiar de direcci�n
        */

        // Env�o al servidor de los inputs del jugador
        ProcessMovementServerRpc(aceleracion, direccion, carController.ID);

    }

    [ServerRpc]
    private void ProcessMovementServerRpc(float aceleracion, float direccion, int idJugador)
    {
        /*
        // Se obtienen todos los jugadores de la escena
        GameObject[] listaJugadores = GameObject.FindGameObjectsWithTag("Player");
        // Se recorren todos los jugadores, hasta que se encuentra aquel cuyo id coincide con el del que ha enviado el mensaje
        foreach(GameObject jugador in listaJugadores)
        {
            Player p = jugador.GetComponent<Player>();
            if(p.ID == idJugador)
            {
                // Se procesan la aceleraci�n y la direcci�n
                CarController cocheJugador = jugador.GetComponentInChildren<CarController>();
                cocheJugador.InputAcceleration = aceleracion;
                cocheJugador.InputSteering = direccion * 0.5f; // Para disminuir la brusquedad al cambiar de direcci�n
                Debug.Log("El coche " + idJugador + " debe tener una aceleraci�n de " + aceleracion + " y una direcci�n de " + direccion);
                // Se sincroniza la posici�n con todos los clientes
                UpdatePositionClientRpc(car.transform.position, car.transform.rotation, idJugador);
            }
        }
        */

        if(carController.ID == idJugador)
        {
            carController.InputAcceleration = aceleracion;
            carController.InputSteering = direccion * 0.5f; // Para disminuir la brusquedad al cambiar de direcci�n
            Debug.Log("El coche " + idJugador + " debe tener una aceleraci�n de " + aceleracion + " y una direcci�n de " + direccion);
            // Se sincroniza la posici�n con todos los clientes
            UpdatePositionClientRpc(car.transform.position, car.transform.rotation, idJugador);
        }

    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 posicionCoche, Quaternion rotacionCoche, int idJugador)
    {
        if (carController.ID == idJugador)
        {
            car.transform.position = posicionCoche;
            car.transform.rotation = rotacionCoche;
            Debug.Log("El coche " + idJugador + " debe tener una aceleraci�n de " + aceleracion + " y una direcci�n de " + direccion);
        }

    }

    /*
    [ClientRpc]
    private void UpdatePositionClientRpc(float aceleracion, float direccion, int idJugador)
    {
        // Se obtienen todos los jugadores de la escena
        GameObject[] listaJugadores = GameObject.FindGameObjectsWithTag("Player");
        // Se recorren todos los jugadores, hasta que se encuentra aquel cuyo id coincide con el del que ha enviado el mensaje
        foreach (GameObject jugador in listaJugadores)
        {
            Player p = jugador.GetComponent<Player>();
            if (p.ID == idJugador)
            {
                // Se procesan la aceleraci�n y la direcci�n
                CarController cocheJugador = jugador.GetComponentInChildren<CarController>();
                cocheJugador.InputAcceleration = aceleracion;
                cocheJugador.InputSteering = direccion * 0.5f; // Para disminuir la brusquedad al cambiar de direcci�n
                Debug.Log("El coche " + idJugador + " debe tener una aceleraci�n de " + aceleracion + " y una direcci�n de " + direccion);
            }
        }
    }
    */

}
