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
    // Player Info
    public string Name;
    public int ID;

    // Materiales del coche
    public Material[] materialesCoche;
    public GameObject body;

    // Race Info
    public GameObject car;
    // Controlador del coche 
    public CarController carController;

    public int CurrentPosition;
    public int CurrentLap;

    // Velocidad
    public float speed;

    // Modelo del coche
    public GameObject[] modeloCoche;

    public override string ToString()
    {
        return Name;
    }

    private void Start()
    {
        GameManager.Instance.currentRace.AddPlayer(this); //para que me siga la pelota y me determine mi orden de carrera

        // Con esto cogemos el ID del player para as� tener la ID de la esfera y poder coger su posici�n
        carController = car.GetComponent<CarController>();
        ID = (int) OwnerClientId;

        // Se obtiene el nombre del jugador
        Dictionary<string, List<string>> datosJugadores = TestLobby.Instance.GetPlayersInLobby();
        datosJugadores.TryGetValue("Nombres", out List<string> nombres);
        Name = nombres[ID];
        if (RaceController.instance.clasificacion)
        {
            InvisibilizarCoches();
        }

        // Al aparecer, se hace que la c�mara siga al coche
        if (!IsOwner) return; // La camara sigue su propio objeto player no el de lo dem�s jugadores

        carController.ID = ID;
        GameObject.FindGameObjectWithTag("FollowCamera").GetComponent<CinemachineVirtualCamera>().Follow = car.transform;
        GameObject.FindGameObjectWithTag("FollowCamera").GetComponent<CinemachineVirtualCamera>().LookAt = car.transform;

    }

    // Funci�n que modifica el color del coche en funci�n de lo escogido en el men�
    public void SetColor(int idColor)
    {
        MeshRenderer meshRendererBody;

        meshRendererBody = body.GetComponent<MeshRenderer>();

        Material[] materialAntiguo = meshRendererBody.sharedMaterials;

        materialAntiguo[0] = materialesCoche[idColor];
        materialAntiguo[1] = materialesCoche[idColor];

        meshRendererBody.sharedMaterials = materialAntiguo;
    }

    public void InvisibilizarCoches()
    {
        if (!IsOwner)
        {
            foreach(GameObject g in modeloCoche)
            {
                g.GetComponentInChildren<MeshRenderer>().enabled = false;
            }

        }
    }

    public void VisibilizarCoches()
    {
        if (!IsOwner)
        {
            foreach (GameObject g in modeloCoche)
            {
                g.GetComponentInChildren<MeshRenderer>().enabled = true;
            }
        }
    }
}
