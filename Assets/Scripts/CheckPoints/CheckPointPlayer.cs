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
        if (checkPointIndex == (currentCheckPointIndex + 1) % CheckPointManager.instance.TotalCheckPoints)
        {
            Debug.Log("Has pasado al siguiente nextPoint");
            currentCheckPointIndex = checkPointIndex;
        }
        else
        {
            ReSpawned();
        }
    }

    public void ReSpawned()
    {
        Vector3 lastCheckpointPosition = CheckPointManager.instance.GetCheckpointPosition(currentCheckPointIndex); //coge la posición del último checkpoint
        transform.position = lastCheckpointPosition;
    }
}
