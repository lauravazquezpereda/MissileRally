using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    NetManager myNetworkManager;

    private void Awake()
    {
        myNetworkManager = GetComponent<NetManager>();
    }

    public void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    //Funci�n que se ejecuta cuando se pulsa el bot�n
    public void OnConnectButtonClicked()
    {
        // Verificar si ya hay un host
        if (NetworkManager.Singleton.IsHost == false)
        {
            Debug.Log("Host Iniciado");
            NetworkManager.Singleton.StartHost();
        }
        
        // Si ya hay un host, simplemente iniciamos el cliente
        else
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    //Cuando se conecte el cliente, se spawnea el jugador
    void OnClientConnected(ulong clientId)
    {
        Debug.Log("Entro en SpawnCar");
        
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            myNetworkManager.SpawnCar(clientId);
            // Desuscribir el callback despu�s de la conexi�n y el spawn
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}
