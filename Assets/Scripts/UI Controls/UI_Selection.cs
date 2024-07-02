using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Selection : MonoBehaviour
{
    // Este script se encarga de controlar el primer menú, en el que se introduce el nombre del jugador y se selecciona el color del coche
    // Variables que controlan si ya se han hecho las dos cosas necesarias
    private bool nombreIntroducido = false;
    private bool colorElegido = false;
    // Botón para continuar, que al principio se muestra invisible
    [SerializeField] GameObject botonContinuar;
    // Referencia al canvas actual para poder ocultarlo
    [SerializeField] GameObject canvasMenu;
    // Referencia al canvas del lobby para poder mostrarlo
    [SerializeField] GameObject canvasLobby;

    // Función para introducir el nombre en el espacio de texto
    public void IntroducirNombre(string name)
    {
        nombreIntroducido = true;
        Debug.Log(name);

        // Se llama al Singleton encargado de gestionar el Lobby, para almacenar el nombre del jugador, para cuando se quiera unir a una sala
        TestLobby.Instance.ModifyNamePlayer(name);
    }
    // Dependiendo del botón que se selccione, se escogerá un color u otro
    public void SeleccionarColor(int color)
    {
        colorElegido = true;
        // Esta decisión se envía al Lobby, para que cuando el jugador se registre, sus datos sean los que 
        // haya elegido en este menú
        switch(color)
        {
            case 0:
                TestLobby.Instance.ModifyColor("red");
                Debug.Log("Red");
                break;
            case 1:
                TestLobby.Instance.ModifyColor("yellow");
                Debug.Log("Yellow");
                break;
            case 2:
                TestLobby.Instance.ModifyColor("green");
                Debug.Log("Green");
                break;
            case 3:
                TestLobby.Instance.ModifyColor("blue");
                Debug.Log("Blue");
                break;
            case 4:
                TestLobby.Instance.ModifyColor("orange");
                Debug.Log("Orange");
                break;
        }
        // Se le pasa el id del color obtenido en base a la opción escogida en el menú
        NetManager.instance.ModifyPrefabColor(color);
    }

    private void Update()
    {
        // Si se ha introducido un nombre y un color, se muestra el botón que permite avanzar al siguiente menú
        if(nombreIntroducido && colorElegido)
        {
            botonContinuar.SetActive(true);
        }
    }

    public void Continuar()
    {
        canvasLobby.SetActive(true);
        canvasMenu.SetActive(false);
    }

}
