using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CheckPointPlayer : NetworkBehaviour
{
    public int currentCheckPointIndex = 0;

    [ServerRpc(RequireOwnership = false)]
    public void OnCheckPointPassedServerRpc(int checkPointIndex)
    {
        Debug.Log("Intentando acceder al checkpoint: " + checkPointIndex);
        if (checkPointIndex == (currentCheckPointIndex + 1) % CheckPointManager.instance.TotalCheckPoints)
        {
            Debug.Log("Has pasado al siguiente nextPoint: "+checkPointIndex);
            currentCheckPointIndex = checkPointIndex;
        }
        else
        {
            ReSpawned();
        }
    }

    public void ReSpawned()
    {
        Debug.Log("Volviendo al chekpoint anterior: " + currentCheckPointIndex);
        Vector3 lastCheckpointPosition = CheckPointManager.instance.GetCheckpointPosition(currentCheckPointIndex); // Coge la posición del último checkpoint
        transform.position = lastCheckpointPosition;
    }
}
