using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPointManager : MonoBehaviour
{
    public static CheckPointManager instance;
    public int TotalCheckPoints = 0; // Variable que almacena el n�mero de CheckPoints del circuito en el que se est�

    // Una lista de checkPoints por cada circuito
    // Dependiendo del circuito escogido, se seleccionan unos u otros
    public List<CheckPoint> checkPoints;
    public List<CheckPoint> checkPoints1;
    public List<CheckPoint> checkPoints2;
    public List<CheckPoint> checkPoints3;
    public List<CheckPoint> checkPoints4;

    public GameObject fundidoNegro;

    public void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    // Conseguimos el �ndice del checkpoint en la lista
    public int GetCheckpointIndex(CheckPoint checkpoint)
    {
       return checkPoints.IndexOf(checkpoint);
    }

    // Conseguimos la posici�n del punto de control, en funci�n de su �ndice
    public Vector3 GetCheckpointPosition(int index)
    {
        return checkPoints[index].transform.position;
    }

    // Se obtiene la rotaci�n del punto de control, en funci�n de su �ndice
    public Quaternion GetCheckpointRotation(int index)
    {
        return checkPoints[index].transform.rotation;
    }
}
