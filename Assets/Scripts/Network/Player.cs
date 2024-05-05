using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    // Player Info
    public string Name { get; set; }
    public int ID { get; set; }

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

        // Con esto cogemos el ID del player para así tener la ID de la esfera y poder coger su posición
        carController = car.GetComponent<CarController>();
        carController.ID = ID;
        // Al aparecer, se hace que la cámara siga al coche
        GameObject.FindGameObjectWithTag("FollowCamera").GetComponent<CinemachineVirtualCamera>().Follow = car.transform;
        GameObject.FindGameObjectWithTag("FollowCamera").GetComponent<CinemachineVirtualCamera>().LookAt = car.transform;
    }

}

//para agregarme a la carrera solo tengo que aparecer