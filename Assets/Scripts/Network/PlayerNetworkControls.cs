using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkControls : NetworkBehaviour
{
    public GameObject car;
    public CarController carController;
    // Esta variable se utiliza para almacenar la velocidad del coche, que se recibe del servidor
    private float speed;
    // Estas variables se utilizan para actualizar la posición y la rotación del coche en los clientes
    private Vector3 posicionCoche;
    private Quaternion rotacionCoche;

    // Start is called before the first frame update
    void Start()
    {
        // Asigna el controlador para manejar el coche
        carController = car.GetComponent<CarController>();
        // Se asegura que se comience frenado
        carController._rigidbody.velocity = Vector3.zero;
        // UI_HUD.Instance.inicioCarrera = true;
        UI_Clasificacion.instance.inicioCarrera = true;
        // Se indica al controlador del coche que es el Owner
        if (!IsOwner) return;
        carController.IsOwner = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return; // Solo el jugador propietario puede controlar su coche
        if (carController.esperandoClasificacion) return; // Si está esperando a que los demás coches terminen la clasificación, no recibe inputs
        // Si se ha terminado la carrera, ya no se reciben más inputs
        if (EndingController.Instance.carreraFinalizada)
        {
            if(NetworkManager.Singleton.IsConnectedClient)
            {
                ProcessMovementServerRpc(0, 0, carController.ID);
            }
            return;
        }
        float acceleration, steering;

        // Captura la entrada de aceleración y dirección
        acceleration = Input.GetAxis("Vertical");
        steering = Input.GetAxis("Horizontal");
        
        // Procesa el movimiento del coche en el servidor
        ProcessMovementServerRpc(acceleration, steering, carController.ID);

        // Modifica el HUD de la velocidad, con la velocidad recibida del servidor. Se actualiza en un script o en otro dependiendo de si se está en la vuelta de clasificación o no
        if(RaceController.instance.clasificacion)
        {
            UI_Clasificacion.instance.ModificarVelocimetro(speed);
        }
        else
        {
            UI_HUD.Instance.ModificarVelocimetro(speed);
        }
    }

    private void FixedUpdate()
    {
        // DEAD RECKONING
        // Corrige la posición del coche, interpolando la posición calculada mediante la predicción en el cliente y la recibida del servidor
        car.transform.position = Vector3.Lerp(car.transform.position, posicionCoche, Time.deltaTime);
        // Se actualiza la rotación del coche, con lo recbido del servidor, para evitar que quede volcado. Si es parte de la carrera, se interpola
        // Si no, se hace directamente
        if(carController.volviendoCheckpoint)
        {
            car.transform.rotation = rotacionCoche;
        }
        else
        {
            car.transform.rotation = Quaternion.Lerp(car.transform.rotation, rotacionCoche, Time.deltaTime);
        }

    }

    [ServerRpc(RequireOwnership = false)]
    void ProcessMovementServerRpc(float acceleration, float steering, int playerId)
    {
        float currentSpeed = 0f;

        foreach (PlayerNetwork player in GameManager.Instance.currentRace._players)
        {
            if (player.ID == playerId)
            {
                // Actualiza la entrada de aceleración y dirección en el jugador correspondiente, para que se mueva adecuadamente
                CarController playerCar = player.GetComponentInChildren<CarController>();
                if(playerCar != null)
                {
                    playerCar.InputAcceleration = acceleration;
                    playerCar.InputSteering = steering * 0.5f; // Reducir la brusquedad al cambiar de dirección
                    currentSpeed = playerCar._currentSpeed; // Se obtiene la velocidad actual del coche, para después transmitirla a los clientes
                    posicionCoche = playerCar.transform.position;
                    rotacionCoche = playerCar.transform.rotation;
                }
            }
        }

        UpdateSpeedClientRpc(currentSpeed, posicionCoche, rotacionCoche, playerId, acceleration, steering);
    }

    // A través de esta función, el cliente recibe el valor de la velocidad del coche en el servidor, para poder modificar el HUD en consecuencia
    [ClientRpc]
    void UpdateSpeedClientRpc(float currentSpeed, Vector3 posicion, Quaternion rotacion, int playerId, float ac, float st)
    {
        if(carController == null)
        {
            Debug.Log("CarController nulo");
            return;
        }
        // Se hace una predicción en el cliente utilizando los parámetros recibidos del servidor
        // Además, se reciben la posición y rotación correctas 
            if (carController.ID == playerId)
            {
                posicionCoche = posicion;
                rotacionCoche = rotacion;
                speed = currentSpeed;
                carController.InputAcceleration = ac;
                carController.InputSteering = st * 0.5f;
            }        
    }
    

    
}


