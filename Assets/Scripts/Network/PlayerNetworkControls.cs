using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkControls : NetworkBehaviour
{
    // Se utiliza una referencia al objeto coche del jugador, que es lo que va a controlar y se va a mover
    public GameObject car;
    public CarController carController; // Por ello, es necesario guardar el controlador que lo maneja, para poder aplicarle los inputs
    // Esta variable se utiliza para almacenar la velocidad del coche, que se recibe del servidor
    private float speed;
    // Estas variables se utilizan para actualizar la posición y la rotación del coche en los clientes recibidos del servidor, para llevar a cabo la interpolación (dead reckoning)
    private Vector3 posicionCoche;
    private Quaternion rotacionCoche;

    // Start is called before the first frame update
    void Start()
    {
        // Asigna el controlador para manejar el coche
        carController = car.GetComponent<CarController>();
        // Se asegura que se comience frenado
        carController._rigidbody.velocity = Vector3.zero;
        // Se comunica a la UI de la clasificación que se ha iniciado la carrera
        UI_Clasificacion.instance.inicioCarrera = true;
        // Se indica al controlador del coche que es el Owner
        if (!IsOwner) return;
        // La posición y rotación de inicio será la del spawn
        posicionCoche = transform.position;
        rotacionCoche = transform.rotation;
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
            // Si se sigue estando conectado, mientras que el coche está parado, envía dicha información al servidor, para que permanezca quieto
            if(NetworkManager.Singleton.IsConnectedClient)
            {
                ProcessMovementServerRpc(0, 0, carController.ID);
            }
            return;
        }
        // Se declaran la aceleración y la dirección
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
        // Se garantiza que el coche se esté quieto mientras se prepara la salida
        if(UI_Circuit.instance.preparandoSalida || UI_HUD.Instance.preparandoCarrera)
        {
            ResetBody();
        }
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
        // El servidor comprueba de quién se están recibiendo inputs
        // Esta función no genera problemas de concurrencia, ya que, cada cliente sólo va a poder mandar los inputs del coche que controla, por lo que no se solapan
        // Si el ID coincide con el que se ha recibido, quiere decir que se trata del coche correcto
        if (carController.ID == playerId)
        {
            // Actualiza la entrada de aceleración y dirección en el jugador correspondiente, para que se mueva adecuadamente
            // Para ello, se necesita su controlador           
            // Se asignan la aceleración y la dirección obtenidas
            carController.InputAcceleration = acceleration;
            carController.InputSteering = steering * 0.5f; // Reducir la brusquedad al cambiar de dirección
            float currentSpeed = carController._currentSpeed; // Se obtiene la velocidad actual del coche, para después transmitirla a los clientes y que puedan actualizar su HUD
            // Se capturan la posición y rotación instantáneas, para que los clientes puedan llevar a cabo la interpolación
            posicionCoche = carController.transform.position;
            rotacionCoche = carController.transform.rotation;                
            // Se comunican todos los parámetros a los clientes, para que actualicen correctamente el estado del coche que se ha modificado con los inputs
            UpdateSpeedClientRpc(currentSpeed, posicionCoche, rotacionCoche, playerId, acceleration, steering);
        }
        
    }

    // A través de esta función, el cliente recibe el valor de la velocidad del coche en el servidor, para poder modificar el HUD en consecuencia
    [ClientRpc]
    void UpdateSpeedClientRpc(float currentSpeed, Vector3 posicion, Quaternion rotacion, int playerId, float ac, float st)
    {
        // Para evitar errores, no se procesa la información si el controlador del coche ha sido eliminado
        if(carController == null)
        {
            Debug.Log("CarController nulo");
            return;
        }
        // Se hace una predicción en el cliente utilizando los parámetros recibidos del servidor (aceleración y dirección)
        // Además, se reciben la posición y rotación correctas, para llevar a cabo la interpolación
        if (carController.ID == playerId)
        {
            posicionCoche = posicion;
            rotacionCoche = rotacion;
            speed = currentSpeed; // Se recibe además la velocidad, para poder actualizar el velocímetro en caso de que se trate del coche propietario
            carController.InputAcceleration = ac;
            carController.InputSteering = st * 0.5f;
        }        
    }

    private void ResetBody()
    {
        carController._rigidbody.velocity = Vector3.zero;
        carController._rigidbody.angularVelocity = Vector3.zero;
        carController._rigidbody.Sleep();
        carController.InputAcceleration = 0;
        carController.InputSteering = 0;
        carController._currentSpeed = 0;
    }
    
}


