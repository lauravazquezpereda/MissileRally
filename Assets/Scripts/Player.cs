using UnityEngine;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    // Player Info
    public string Name { get; set; }
    public int ID { get; set; }

    // Race Info
    public GameObject car;

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
        carController.ID = ID;
    }

}

//para agregarme a la carrera solo tengo que aparecer