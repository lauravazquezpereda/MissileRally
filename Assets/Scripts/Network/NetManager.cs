using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetManager : MonoBehaviour
{
    public static NetManager instance;

    [SerializeField] private List<Transform> posCoche;
    [SerializeField] private GameObject playerPrefab;

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

    private void OnDestroy()
    {
        // Desuscribir el evento para evitar errores de referencia nula
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("Client connected with ID: " + clientId);

        // Solo el servidor debería manejar la generación del coche
        if (NetworkManager.Singleton.IsServer)
        {
            SpawnCar(clientId);
        }
    }

    public void SpawnCar(ulong clientId)
    {
        Vector3 spawnPosition = posCoche[(int)clientId].position; //posición en la que spawnea el coche

        //Instanciamos el objeto en la escena en la posición de arriba
        GameObject playerObj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        RaceController.instance.AddPlayer(playerObj.GetComponent<PlayerNetwork>()); //añadimos los jugadores a la lista
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

