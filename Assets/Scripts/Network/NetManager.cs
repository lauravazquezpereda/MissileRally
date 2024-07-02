using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

public class NetManager : MonoBehaviour
{
    // Se hace que esta clase sea un Singleton, ya que es un controlador que almacena informaci�n muy importante a la que se necesita acceder desde distintas partes del c�digo
    public static NetManager instance;
    // En esta lista se almacenan las 4 posibles posiciones de inicio para los coches, para los 4 circuitos, ordenadas seg�n el identificador de dicho circuito
    public List<Transform> posCoche;
    [SerializeField] private GameObject playerPrefab; // Prefab del jugador, para poder spawnear los objetos en red
    // Se necesita almacenar una referencia del circuito que se ha escogido, para asignar correctamente la posici�n de inicio de los coches de la lista anterior
    public int circuitoSeleccionado;
    public int numClients = 0; // N�mero de clientes conectados
    // El n�mero total de posibles posiciones de cada circuito, es necesario para hallar la posici�n correcta conociendo la posici�n en la que debe spawnear cada coche, junto
    // con el id del circuito escogido
    private const int NUM_POSICIONES_CIRCUITO = 4;
    // En esta lista se almacenan las posiciones en las que han quedado los jugadores en la clasificaci�n, para poder colocar los coches en la carrera dependiendo de esto
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
        // Se suscribe al evento que se invoca cada vez que se conecta un cliente
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }
    // Esta funci�n se ejecuta cada vez que se conecta un cliente
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("Client connected with ID: " + clientId);
        // Se incrementa el n�mero de clientes conectados
        numClients++;
    }
    // Esta funci�n se utiliza para generar los coches de los jugadores, una vez se ha escogido el circuito
    // Se ejecuta al inicio de la clasificaci�n
    public void GeneratePlayersInCircuit()
    {
        // Se indica al controlador de la carrera que esta ya ha comenzado
        RaceController.instance.carreraIniciada = true;
        // S�lo el servidor puede encargarse de spawnear los coches, al hacerlo aparecer�n en el resto de clientes directamente
        if (NetworkManager.Singleton.IsServer)
        {
            // Se genera un coche para cada cliente
            for (int i = 0; i < numClients; i++)
            {
                SpawnCar((ulong)i);
            }
        }
    }
    // Esta funci�n es muy similar que la anterior, solo que se ejecuta al inicio de la carrera, tras la clasificaci�n
    public void GeneratePlayersOrderedInCircuit()
    {
        RaceController.instance.carreraIniciada = true;

        if (NetworkManager.Singleton.IsServer)
        {
            // Se genera un coche para cada cliente, pero, con la particularidad de que debe ser en una posici�n ordenada seg�n la clasificaci�n
            for (int i = 0; i < numClients; i++)
            {
                SpawnOrderedCar((ulong)i);
            }
        }

    }

    public void SpawnCar(ulong clientId)
    {
        // Se vuelve a verificar que quien est� intentando ejecutar la funci�n es el servidor, para evitar errores
        if (NetworkManager.Singleton.IsServer)
        {
            // Se calcula la posici�n de generaci�n en base al ID del cliente, ya que, no influir� para la clasificaci�n, que comienza a contar una vez se sobrepasa la l�nea de meta
            Vector3 spawnPosition = posCoche[(int)clientId + (circuitoSeleccionado * NUM_POSICIONES_CIRCUITO)].position; // Se suma el ID de la posici�n y se aplica un factor que salta
            // las posiciones de los circuitos anteriores, multiplicando por 4, que es el n�mero de posiciones que hay en cada circuito
            Quaternion spawnRotation = Quaternion.identity;
            // Se instancia el objeto en local
            GameObject playerObj = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            // Utilizando la referencia al objeto, se obtiene su componente Network, para crearlo en el resto de instancias de clientes
            playerObj.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);

        }

    }

    public void SpawnOrderedCar(ulong clientId)
    {
        // Primero se aplica un algoritmo de b�squeda, para averiguar en qu� posicion se encuentra el cliente con el ID proporcionado, para saber d�nde colocarlo en la salida
        int posicionClasificacion = -1;
        for (int i = 0; i < posicionesClasificacion.Count; i++)
        {
            // Si el ID coincide con el de la lista, quiere decir que la posici�n es su resultado de la clasificaci�n
            if ((int)clientId == posicionesClasificacion[i])
            {
                posicionClasificacion = i;
            }
        }
        // El procedimiento es igual al de la funci�n anterior, con la excepci�n de que en vez de utilizar el id del cliente, se usa la posici�n conseguida en la clasificaci�n
        Vector3 spawnPosition = posCoche[posicionClasificacion + (circuitoSeleccionado * NUM_POSICIONES_CIRCUITO)].position;
        Quaternion spawnRotation = Quaternion.identity;

        GameObject playerObj = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        playerObj.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
    }
    // Esta funci�n se utiliza para modificar el color del prefab del jugador en funci�n de lo escogido en el men� de inicio (color del propietario)
    public void ModifyPrefabColor(int idColor)
    {
        // Se accede al componente Player del prefab, para modificar su color con la funci�n de dicho script
        PlayerNetwork p = playerPrefab.GetComponent<PlayerNetwork>();
        if (p != null)
        {
            p.SetColor(idColor);
            Debug.Log("Cambiando color");
        }
    }

    // Mediante este m�todo se desactivan las colisiones entre los jugadores en la vuelta de clasificaci�n, para que la puedan hacer en cualquier orden y sin interferirse entre ellos
    public void IgnorarColisionesEntreJugadores(bool ignorar)
    {
        // Se obtienen todos los jugadores de la escena, ya que tienen la etiqueta Player asignada
        GameObject[] jugadores = GameObject.FindGameObjectsWithTag("Player");
        // Se ignoran las colisiones con el objeto coche
        // Se hacen dos bucles para cubrir todas las combinaciones posibles, sin que importe el orden
        for (int i = 0; i < jugadores.Length; i++)
        {
            for (int j = i + 1; j < jugadores.Length; j++)
            {
                // Se obtienen los box collider del coche
                BoxCollider col1 = jugadores[i].GetComponentInChildren<BoxCollider>();
                BoxCollider col2 = jugadores[j].GetComponentInChildren<BoxCollider>();
                // Se ignora la colisic�n entre ambos, en ambos sentidos
                Physics.IgnoreCollision(col1, col2, ignorar);
                Physics.IgnoreCollision(col2, col1, ignorar);
            }
        }
        // Se ignoran las colisiones de las ruedas
        // Para ello, se aplican todas las combinaciones posibles, importando el orden
        for (int i=0; i< jugadores.Length; i++)
        {
            for(int j=0; j<jugadores.Length; j++)
            {
                // El �nico caso en el que no se ejecuta es cuando estemos desactivando las colisiones entre coche y ruedas de un mismo jugador
                if(i!=j)
                {
                    BoxCollider col1 = jugadores[i].GetComponentInChildren<BoxCollider>();
                    // Se obtienen los colisionadores de las ruedas (4 por coche, por eso en una lista)
                    WheelCollider[] ruedas = jugadores[j].GetComponentsInChildren<WheelCollider>();
                    // Se ignora la colisi�n con cada una de las ruedas, para ello se recorre la lista
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

