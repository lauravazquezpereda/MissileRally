using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetManager : MonoBehaviour
{
    [SerializeField] private List<Transform> posCoche;
    [SerializeField] private GameObject playerPrefab;


    private void Start()
    {
        // Suscribir el evento de cliente conectado
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        // Desuscribir el evento para evitar errores de referencia nula
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("Client connected with ID: " + clientId);
    }

    public void SpawnCar(ulong clientId)
    {
        Vector3 spawnPosition = posCoche[(int)clientId].position; //posicion en la que spawnea el coche

        //Instanciamos el objecto en la escena en la posición de arriba
        GameObject playerObj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        RaceController.instance.AddPlayer(playerObj.GetComponent<PlayerNetwork>()); //añadimos los jugadores a la lista
    }
}
