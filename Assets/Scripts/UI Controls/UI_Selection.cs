using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Selection : MonoBehaviour
{
    private bool nombreIntroducido = false;
    private bool colorElegido = false;

    [SerializeField] GameObject botonContinuar;
    [SerializeField] GameObject canvasMenu;
    [SerializeField] GameObject canvasLobby;

    public void IntroducirNombre(string name)
    {
        nombreIntroducido = true;
        Debug.Log(name);

        // Se llama al Singleton encargado de gestionar el Lobby, para almacenar el nombre del jugador, para cuando se quiera unir a una sala
        TestLobby.Instance.ModifyNamePlayer(name);
    }

    public void SeleccionarColor(int color)
    {
        colorElegido = true;
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
