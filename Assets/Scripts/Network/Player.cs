using Cinemachine;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class Player: NetworkBehaviour
{
    // Player Info
    public string Name { get; set; }
    public int ID { get; set; }

    // Materiales del coche
    public Material[] materialesCoche;
    public GameObject body;

    // Race Info
    public GameObject car;
    // Controlador del coche 
    private CarController carController;

    public int CurrentPosition { get; set; }
    public int CurrentLap { get; set; }

    public override string ToString()
    {
        return Name;
    }

    private void Start()
    {
        GameManager.Instance.currentRace.AddPlayer(this); //para que me siga la pelota y me determine mi orden de carrera

        // Con esto cogemos el ID del player para as� tener la ID de la esfera y poder coger su posici�n
        carController = car.GetComponent<CarController>();
        ID = (int)OwnerClientId;

        // Al aparecer, se hace que la c�mara siga al coche
        if (!IsOwner) return; // La camara sigue su propio objeto player no el de lo dem�s jugadores
        
        carController.ID = ID;
        GameObject.FindGameObjectWithTag("FollowCamera").GetComponent<CinemachineVirtualCamera>().Follow = car.transform;
        GameObject.FindGameObjectWithTag("FollowCamera").GetComponent<CinemachineVirtualCamera>().LookAt = car.transform;

        // Llama a la funci�n SetColor() para seleccionar el color del coche seg�n el ID del jugador
        SetColor();
    }

    private void SetColor()
    {
        MeshRenderer meshRendererBody;

        meshRendererBody = body.GetComponent<MeshRenderer>();

        Material[] materialAntiguo = meshRendererBody.materials;

        materialAntiguo[0] = materialesCoche[ID];
        materialAntiguo[1] = materialesCoche[ID];

        meshRendererBody.materials = materialAntiguo;
    }

}

//para agregarme a la carrera solo tengo que aparecer