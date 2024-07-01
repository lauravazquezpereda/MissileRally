using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CheckPointPlayer : NetworkBehaviour
{
    public int currentCheckPointIndex = 0;
    public bool restaurandoPosicion = false;

    [ServerRpc(RequireOwnership = false)]
    public void OnCheckPointPassedServerRpc(int checkPointIndex, int id)
    {
        // Se comprueba que el coche no esté detenido esperando a que acabe la carrera o la clasificación
        CarController carController = GetComponent<CarController>();
        if(carController != null)
        {
            if(carController.esperandoClasificacion)
            {
                return;
            }
        }
        // Se detecta que el coche está parado, pero como no se ha comenzado a mover, no se traslada
        if(checkPointIndex == -1 && currentCheckPointIndex == -1)
        {
            return;
        }
        Debug.Log("Intentando acceder al checkpoint: " + checkPointIndex);
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

    public void ReSpawned(int id)
    {
        Debug.Log("Volviendo al chekpoint anterior: " + currentCheckPointIndex);
        Vector3 lastCheckpointPosition = CheckPointManager.instance.GetCheckpointPosition(currentCheckPointIndex); // Coge la posición del último checkpoint
        Quaternion lastaCheckpointRotation = CheckPointManager.instance.GetCheckpointRotation(currentCheckPointIndex);
        CarController car = GetComponent<CarController>();
        RespawnClientRpc(lastCheckpointPosition, lastaCheckpointRotation, id);
        StartCoroutine(RespawnSequenceServer(lastCheckpointPosition, lastaCheckpointRotation));
    }

    [ClientRpc]
    public void RespawnClientRpc(Vector3 posicion, Quaternion rotacion, int id)
    {
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

    private IEnumerator RespawnSequence(Vector3 posicion, Quaternion rotacion)
    {
        restaurandoPosicion = true;

        // Fundido a negro
        yield return StartCoroutine(FadeController.instance.FadeOut());

        // Resetear las físicas del coche
        CarController car = GetComponent<CarController>();
        ResetCarPhysics(car);

        // Reasignar la posición del coche
        transform.position = posicion;
        transform.rotation = rotacion;

        // Esperar dos segundos con la pantalla en negro
        yield return new WaitForSeconds(2f);

        // Fundido desde negro
        yield return StartCoroutine(FadeController.instance.FadeIn());

        restaurandoPosicion = false;
        car.volviendoCheckpoint = false;
    }

    private IEnumerator RespawnSequenceServer(Vector3 posicion, Quaternion rotacion)
    {
        restaurandoPosicion = true;

        // Espera del supuesto fundido del cliente
        yield return new WaitForSeconds(FadeController.instance.fadeDuration);

        // Resetear las físicas del coche
        CarController car = GetComponent<CarController>();
        ResetCarPhysics(car);

        // Reasignar la posición del coche
        transform.position = posicion;
        transform.rotation = rotacion;

        // Esperar dos segundos con la pantalla en negro
        yield return new WaitForSeconds(2f);

        // Espera del supuesto fundido del cliente
        yield return new WaitForSeconds(FadeController.instance.fadeDuration);

        restaurandoPosicion = false;
        car.volviendoCheckpoint = false;
    }

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


    [ClientRpc]
    public void OnCheckPointPassedClientRpc(int checkPointIndex)
    {
        if (!IsOwner) return;
        OnCheckPointPassedServerRpc(checkPointIndex, (int)OwnerClientId);
    }
}
