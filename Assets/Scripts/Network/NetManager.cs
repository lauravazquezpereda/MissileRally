using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetManager : NetworkBehaviour
{
    public static NetManager instance;

    [SerializeField] private List<Transform> posCoche;
    [SerializeField] private GameObject playerPrefab;

    public int circuitoSeleccionado;
    public int numClients = 0;

    private const int NUM_CIRCUITOS = 4;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        // Suscribir el evento de cliente conectado
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("Client connected with ID: " + clientId);

        numClients++;

        // Solo el servidor debería manejar la generación del coche
        /*
        if (NetworkManager.Singleton.IsServer)
        {
            SpawnCar(clientId);
        }
        */
    }

    public void GeneratePlayersInCircuit()
    {
        RaceController.instance.carreraIniciada = true;

        if (NetworkManager.Singleton.IsServer)
        {
            for(int i=0; i<numClients; i++)
            {
                SpawnCar((ulong)i);
            }
        }
    }

    public void SpawnCar(ulong clientId)
    {
        Vector3 spawnPosition = posCoche[(int)clientId + (circuitoSeleccionado * NUM_CIRCUITOS)].position; // posición en la que spawnea el coche - se tiene en cuenta el circuito escogido, ya que las posiciones están ordenadas

        //Instanciamos el objeto en la escena en la posición de arriba
        GameObject playerObj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

    }

    public void ModifyPrefabColor(int idColor)
    {
        // Se accede al componente Player del prefab, para modificar su color
        PlayerNetwork p = playerPrefab.GetComponent<PlayerNetwork>();
        if (p != null)
        {
            p.SetColor(idColor);
            Debug.Log("Cambiando color");
        }
    }
}

