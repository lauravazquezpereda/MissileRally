using Cinemachine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class PlayerNetwork : NetworkBehaviour
{
    // Información del jugador
    public string Name; // Nombre con el que se unió al lobby
    public int ID; // Identificador que se le asignó al conectarse

    // Materiales del coche
    public Material[] materialesCoche; // Todos los posibles colores que puede tener
    public GameObject body; // Referencia al cuerpo del coche para poder modificar su color

    // Referencia al objeto coche
    public GameObject car;
    // Controlador del coche 
    public CarController carController;

    // Información acerca del estado del jugador en la carrera
    public int CurrentPosition;
    public int CurrentLap;

    // Velocidad
    public float speed;

    // Lista con todos los elementos del modelo 3D del coche, para poder ocultarlo en la vuelta de clasificación
    public GameObject[] modeloCoche;

    public override string ToString()
    {
        return Name;
    }

    private void Start()
    {
        // Al ser spawneado, el coche se une automáticamente a la lista de jugadores de la carrera, para poder actualizar su estado (posición de carrera...)
        GameManager.Instance.currentRace.AddPlayer(this);

        // Se obtiene el controlador del coche
        carController = car.GetComponent<CarController>();
        // El ID se consigue mediante el identificador asignado al conectarse el cliente en red
        ID = (int) OwnerClientId;

        // Se obtiene el nombre del jugador, mediante la lista de datos almacenados en el lobby
        Dictionary<string, List<string>> datosJugadores = TestLobby.Instance.GetPlayersInLobby();
        // Se almacenan los nombres utilizando la key en el diccionario
        datosJugadores.TryGetValue("Nombres", out List<string> nombres);
        // Mediante el identificador, se asigna el nombre correcto
        Name = nombres[ID];
        // Si se está en la vuelta de clasificación, no interesa ver los coches de los demás, solo el propio, por lo que, se hace invisible
        if (RaceController.instance.clasificacion)
        {
            InvisibilizarCoches();
        }

        // Al aparecer, se hace que la cámara siga al coche
        if (!IsOwner) return; // La camara sigue su propio objeto player no el de lo demás jugadores
        // Se le asigna el identificador al controlador
        carController.ID = ID;
        // Se hace que la cámara siga y mire hacia el coche
        GameObject.FindGameObjectWithTag("FollowCamera").GetComponent<CinemachineVirtualCamera>().Follow = car.transform;
        GameObject.FindGameObjectWithTag("FollowCamera").GetComponent<CinemachineVirtualCamera>().LookAt = car.transform;

    }

    // Función que modifica el color del coche en función de lo escogido en el menú
    public void SetColor(int idColor)
    {
        // Se obtiene el componente renderizador del coche
        MeshRenderer meshRendererBody = body.GetComponent<MeshRenderer>();
        // A través del componente anterior, se consiguen los materiales antiguos del coche
        Material[] materialAntiguo = meshRendererBody.sharedMaterials;
        // Se ha comprobado que los colores del cuerpo del coche son el primero y el segundo, ya que el tercero es de los cristales
        // Por lo tanto, utilizando la lista de materiales que almacena el conjunto de colores, se modifican los materiales antiguos 
        materialAntiguo[0] = materialesCoche[idColor];
        materialAntiguo[1] = materialesCoche[idColor];
        // Se reasignan los materiales ya modificados
        meshRendererBody.sharedMaterials = materialAntiguo;
    }

    public void InvisibilizarCoches()
    {
        // Interesa invisibilizar todos los coches, menos el del propietario de la build
        if (!IsOwner)
        {
            // Para ello, se desactiva el renderizador de todos los objetos añadidos a la lista del modelo del coche
            foreach(GameObject g in modeloCoche)
            {
                g.GetComponentInChildren<MeshRenderer>().enabled = false;
            }

        }
    }
}
