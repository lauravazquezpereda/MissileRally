using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CheckPointPlayer : NetworkBehaviour
{
    public int currentCheckPointIndex = 0;

    [ServerRpc(RequireOwnership = false)]
    public void OnCheckPointPassedServerRpc(int checkPointIndex, int id)
    {
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
            transform.position = posicion;
            // Solo se hace el fundido si se trata del coche del propietario del juego
            StartCoroutine(RespawnSequence(posicion, rotacion));
        }
        else if((int)OwnerClientId == id)
        {
            // Reposicionar el coche
            CarController car = GetComponent<CarController>();
            car.InputAcceleration = 0;
            car.InputSteering = 0;
            car._currentSpeed = 0;
            transform.position = posicion;
            transform.rotation = rotacion;
        }
    }

    private IEnumerator RespawnSequence(Vector3 posicion, Quaternion rotacion)
    {
        // Fundido a negro
        yield return StartCoroutine(FadeController.instance.FadeOut());

        // Reposicionar el coche
        CarController car = GetComponent<CarController>();
        car.InputAcceleration = 0;
        car.InputSteering = 0;
        car._currentSpeed = 0;
        transform.position = posicion;
        transform.rotation = rotacion;

        // Esperar dos segundos
        yield return new WaitForSeconds(2f);

        // Fundido desde negro
        yield return StartCoroutine(FadeController.instance.FadeIn());
    }

    private IEnumerator RespawnSequenceServer(Vector3 posicion, Quaternion rotacion)
    {
        yield return new WaitForSeconds(2f);
        // Reposicionar el coche
        CarController car = GetComponent<CarController>();
        car.InputAcceleration = 0;
        car.InputSteering = 0;
        car._currentSpeed = 0;
        transform.position = posicion;
        transform.rotation = rotacion;

    }


    [ClientRpc]
    public void OnCheckPointPassedClientRpc(int checkPointIndex)
    {
        if (!IsOwner) return;
        OnCheckPointPassedServerRpc(checkPointIndex, (int)OwnerClientId);
    }
}
