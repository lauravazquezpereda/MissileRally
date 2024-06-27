using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NuevoCircuito : MonoBehaviour
{
    [SerializeField] GameObject canvasFinalJuego;
    [SerializeField] GameObject canvasCircuito;
    public void VolverCircuito()
    {
        canvasFinalJuego.SetActive(false);
        canvasCircuito.SetActive(true);
        UI_Circuit.instance.ResetState();
    }

    public void Volver()
    {
        // Limpiar las credenciales de autenticaci�n antes de iniciar sesi�n, en caso de que se haya iniciado autom�ticamente sesi�n con las mismas credenciales
        AuthenticationService.Instance.ClearSessionToken();
        AuthenticationService.Instance.SignOut();
        // Se resetea la escena
        // Obtener el nombre de la escena actual
        string sceneName = SceneManager.GetActiveScene().name;
        // Recargar la escena
        SceneManager.LoadScene(sceneName);
    }

    

}
