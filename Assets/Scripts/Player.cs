using UnityEngine;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    // Player Info
    public string Name { get; set; }
    public int ID { get; set; }

    // Race Info
    public GameObject car;
    public int CurrentPosition { get; set; }
    public int CurrentLap { get; set; }

    public override string ToString()
    {
        return Name;
    }

    private void Start()
    {
        GameManager.Instance.currentRace.AddPlayer(this); //para que me siga la pelota y me determine mi orden de carrera
    }
}

//para agregarme a la carrera solo tengo que aparecer