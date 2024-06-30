using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

public class NetManager : MonoBehaviour
{
    public static NetManager instance;

    public List<Transform> posCoche;
    [SerializeField] private GameObject playerPrefab;

    public int circuitoSeleccionado;
    public int numClients = 0;

    private const int NUM_CIRCUITOS = 4;

    public List<int> posicionesClasificacion = new List<int>();


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
    }

    public void GeneratePlayersInCircuit()
    {
        RaceController.instance.carreraIniciada = true;

        if (NetworkManager.Singleton.IsServer)
        {
            for (int i = 0; i < numClients; i++)
            {
                SpawnCar((ulong)i);
            }
        }
    }

    public void GeneratePlayersOrderedInCircuit()
    {
        RaceController.instance.carreraIniciada = true;

        if (NetworkManager.Singleton.IsServer)
        {
            for (int i = 0; i < numClients; i++)
            {
                SpawnOrderedCar((ulong)i);
            }
        }

    }

    public void SpawnCar(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Vector3 spawnPosition = posCoche[(int)clientId + (circuitoSeleccionado * NUM_CIRCUITOS)].position;
            Quaternion spawnRotation = Quaternion.identity;

            GameObject playerObj = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            playerObj.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);

        }

    }

    public void SpawnOrderedCar(ulong clientId)
    {
        // Mediante su id se busca la posición que ha ocupado en la lista de clasificación
        int posicionClasificacion = -1;
        for (int i = 0; i < posicionesClasificacion.Count; i++)
        {
            if ((int)clientId == posicionesClasificacion[i])
            {
                posicionClasificacion = i;
            }
        }

        Vector3 spawnPosition = posCoche[posicionClasificacion + (circuitoSeleccionado * NUM_CIRCUITOS)].position;
        Quaternion spawnRotation = Quaternion.identity;

        GameObject playerObj = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        playerObj.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
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

    // Mediante este método se desactivan las colisiones entre los jugadores en la vuelta de clasificación, para que la puedan hacer en cualquier orden
    // Después, se reactivarán para que en la carrera si interfieran entre sí
    public void IgnorarColisionesEntreJugadores(bool ignorar)
    {
        GameObject[] jugadores = GameObject.FindGameObjectsWithTag("Player");
        // Se ignoran las colisiones con el objeto coche
        for (int i = 0; i < jugadores.Length; i++)
        {
            for (int j = i + 1; j < jugadores.Length; j++)
            {
                BoxCollider col1 = jugadores[i].GetComponentInChildren<BoxCollider>();
                BoxCollider col2 = jugadores[j].GetComponentInChildren<BoxCollider>();
                Physics.IgnoreCollision(col1, col2, ignorar);
                Physics.IgnoreCollision(col2, col1, ignorar);
            }
        }
        // Se ignoran las colisiones de las ruedas
        for (int i=0; i< jugadores.Length; i++)
        {
            for(int j=0; j<jugadores.Length; j++)
            {
                if(i!=j)
                {
                    BoxCollider col1 = jugadores[i].GetComponentInChildren<BoxCollider>();
                    WheelCollider[] ruedas = jugadores[j].GetComponentsInChildren<WheelCollider>();
                    // Se ignora la colisión con cada una de las ruedas
                    foreach(WheelCollider wbr in ruedas)
                    {
                        Physics.IgnoreCollision(col1, wbr, ignorar);
                        Physics.IgnoreCollision(wbr, col1, ignorar);
                    }
                }
            }
        }
    }

}

