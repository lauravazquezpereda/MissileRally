using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CheckPointPlayer : NetworkBehaviour
{
    // Este script ser� llevado por cada jugador, para comprobar que est� realizando bien el recorrido, sin saltarse partes del circuito, quedarse volcado/atascado o yendo por donde no debe
    public int currentCheckPointIndex = 0; // Esta variable indica cu�l fue el �ltimo checkpoint que atraves�
    public bool restaurandoPosicion = false; // Esta variable indica que el coche est� siendo desplazado a su �ltimo checkpoint

    [ServerRpc(RequireOwnership = false)]
    public void OnCheckPointPassedServerRpc(int checkPointIndex, int id)
    {
        // Para evitar utilizar otras funciones y gestionar los puntos de control y los traslados desde el mismo lugar, si se vuelca o atacasca, el �ndice del checkpoint recibido ser� -1
        // Se comprueba que el coche no est� detenido esperando a que acabe la carrera o la clasificaci�n. En ese caso, no se le aplicar� el traslado a otro checkpoint
        // Esto ocurrir� porque se detectar� que est� parado, para evitarlo se realiza esta comprobaci�n
        CarController carController = GetComponent<CarController>();
        if(carController != null)
        {
            if(carController.esperandoClasificacion)
            {
                return;
            }
        }
        // Se detecta que el coche est� parado, pero como no se ha comenzado a mover (su checkpoint actual ser� -1), no se traslada
        if(checkPointIndex == -1 && currentCheckPointIndex == -1)
        {
            return;
        }
        Debug.Log("Intentando acceder al checkpoint: " + checkPointIndex);
        // Se comprueba si el checkpoint al que se quiere acceder es el siguiente o el mismo (a veces al respawnear ocurre). En caso de que sea as�, se actualiza la variable
        // que almacena el checkpoint actual. En caso contrario, se respawnea el coche
        if (checkPointIndex == (currentCheckPointIndex + 1) % CheckPointManager.instance.TotalCheckPoints || checkPointIndex == currentCheckPointIndex)
        {
            Debug.Log("Has pasado al siguiente nextPoint: "+checkPointIndex);
            currentCheckPointIndex = checkPointIndex;
        }
        else
        {
            ReSpawned(id);
        }
    }

    // Esta funci�n respawnea al cliente de id dado en el �ltimo checkpoint correcto que pis�
    public void ReSpawned(int id)
    {
        Debug.Log("Volviendo al chekpoint anterior: " + currentCheckPointIndex);
        // La posici�n que tomar� ser� la del punto de control, y la rotaci�n tambi�n, ya que se han colocado de forma que el coche mire hacia donde debe continuar
        Vector3 lastCheckpointPosition = CheckPointManager.instance.GetCheckpointPosition(currentCheckPointIndex); // Coge la posici�n del �ltimo checkpoint
        Quaternion lastaCheckpointRotation = CheckPointManager.instance.GetCheckpointRotation(currentCheckPointIndex);
        // El proceso de reaparici�n se hace tanto en el cliente como en el servidor, para que no haya problemas de sincronizaci�n
        RespawnClientRpc(lastCheckpointPosition, lastaCheckpointRotation, id);
        StartCoroutine(RespawnSequenceServer(lastCheckpointPosition, lastaCheckpointRotation));
    }
    // Esta funci�n se encarga de respawnear el coche en los clientes
    [ClientRpc]
    public void RespawnClientRpc(Vector3 posicion, Quaternion rotacion, int id)
    {
        // Si el cliente es el propietario del coche a desplazar, se inicia una secuencia con un fundido a negro, para dar un mejor resultado visual
        if((int)OwnerClientId == id && IsOwner)
        {
            // Solo se hace el fundido si se trata del coche del propietario del juego
            // Se evita que se haga varias veces
            if (restaurandoPosicion) return;
            StartCoroutine(RespawnSequence(posicion, rotacion));
        }
        else if((int)OwnerClientId == id)
        {
            // Reposicionar el coche
            CarController car = GetComponent<CarController>();
            ResetCarPhysics(car);
            transform.position = posicion;
            transform.rotation = rotacion;
        }
    }
    // Esta secuencia se lleva a cabo en el cliente propietario del coche
    private IEnumerator RespawnSequence(Vector3 posicion, Quaternion rotacion)
    {
        restaurandoPosicion = true;

        // Fundido a negro
        yield return StartCoroutine(FadeController.instance.FadeOut());

        // Resetear las f�sicas del coche
        CarController car = GetComponent<CarController>();
        ResetCarPhysics(car);

        // Reasignar la posici�n del coche
        transform.position = posicion;
        transform.rotation = rotacion;

        // Esperar dos segundos con la pantalla en negro
        yield return new WaitForSeconds(2f);

        // Fundido desde negro
        yield return StartCoroutine(FadeController.instance.FadeIn());

        restaurandoPosicion = false;
        car.volviendoCheckpoint = false; // Se indica esto para el controlador del coche, ya que esto influye en la interpolaci�n de la rotaci�n. Mientras se est� volviendo
        // no se interpola, se toma directamente, para evitar problemas de sincronizaci�n y que el coche aparezca volcado
    }

    // Esta funci�n es la misma secuencia que en el cliente pero para el servidor, con la diferencia de que no se llevan a cabo los fundidos, sino que simplemente se espera el tiempo
    // equivalente para una correcta sincronizaci�n
    private IEnumerator RespawnSequenceServer(Vector3 posicion, Quaternion rotacion)
    {
        restaurandoPosicion = true;

        // Espera del supuesto fundido del cliente
        yield return new WaitForSeconds(FadeController.instance.fadeDuration);

        // Resetear las f�sicas del coche
        CarController car = GetComponent<CarController>();
        ResetCarPhysics(car);

        // Reasignar la posici�n del coche
        transform.position = posicion;
        transform.rotation = rotacion;

        // Esperar dos segundos con la pantalla en negro
        yield return new WaitForSeconds(2f);

        // Espera del supuesto fundido del cliente
        yield return new WaitForSeconds(FadeController.instance.fadeDuration);

        restaurandoPosicion = false;
        car.volviendoCheckpoint = false;
    }
    // Esta funci�n resetea las f�sicas del coche para poder reposicionarlo sin que haya desviaciones por su velocidad o velocidad angular
    private void ResetCarPhysics(CarController car)
    {
        car._rigidbody.velocity = Vector3.zero;
        car._rigidbody.angularVelocity = Vector3.zero;
        car._rigidbody.Sleep();
        car.InputAcceleration = 0;
        car.InputSteering = 0;
        car._currentSpeed = 0;
        car.volviendoCheckpoint = true;
    }

    // Esta funci�n se ejecuta en el cliente cada vez que un coche atraviesa un trigger de un checkpoint
    [ClientRpc]
    public void OnCheckPointPassedClientRpc(int checkPointIndex)
    {
        // Si no es el propietario del coche, no se hace nada
        if (!IsOwner) return;
        // Si lo es, se informa al servidor del checkpoint al que quiere ir
        OnCheckPointPassedServerRpc(checkPointIndex, (int)OwnerClientId);
    }
}
