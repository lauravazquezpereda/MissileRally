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
        if (!IsOwner) return; // Solo puede moverse su propio objeto player no el de lo demás jugadores

        // CONTROLES DEL COCHE //
        aceleracion = Input.GetAxis("Vertical"); // Controles asignados a las teclas WS
        direccion = Input.GetAxis("Horizontal"); // Controles asignados a las teclas AD

        // Asignación de la entrada a los parámetros de los coches
        carController.InputAcceleration = aceleracion;
        carController.InputSteering = direccion * 0.5f; // Para disminuir la brusquedad al cambiar de dirección
    }
}
