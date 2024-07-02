using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        CheckPointPlayer playerCheckpointManager = other.GetComponent<CheckPointPlayer>();

        // Cada vez que se entra en el Trigger de un checkPoint, se llama al método del Player para comprobar que es el correcto
        if (playerCheckpointManager != null)
        {
            // Se ejecuta primero esta función en el cliente, informando que está intentando atravesar un nuevo checkpoint
            playerCheckpointManager.OnCheckPointPassedClientRpc(CheckPointManager.instance.GetCheckpointIndex(this));
        }
    }
}
