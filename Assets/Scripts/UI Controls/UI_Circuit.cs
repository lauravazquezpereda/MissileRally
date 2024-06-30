using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class UI_Circuit : NetworkBehaviour
{
    public static UI_Circuit instance;

    private int circuitoSeleccionado;
    [SerializeField] GameObject botonAceptar;
    [SerializeField] GameObject canvas;
    [SerializeField] GameObject canvasLobbyWaiting;
    [SerializeField] GameObject textoEspera;

    // Variables que s�lo va a almacenar el servidor
    private int[] circuitosEscogidos;
    private int numSelecciones = 0;

    // Lista de circuitos
    public GameObject[] circuitos;

    private bool opcionEnviada = false;
    public bool finalizarMenu = false;

    private bool reiniciarCarrera = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        circuitosEscogidos = new int[4];
    }

    private void Update()
    {
        if(finalizarMenu)
        {
            OcultarMenu();
            finalizarMenu = false;
        }
    }

    public void escogerCircuito(int numCircuito)
    {
        botonAceptar.SetActive(true);
        circuitoSeleccionado = numCircuito;
    }

    public void aceptarSeleccion()
    {
        if (opcionEnviada) return;

        // Asegurarse de que este c�digo se ejecuta en el cliente
        if (!IsClient) return;

        textoEspera.SetActive(true);
        Debug.Log("Cliente " + NetworkManager.Singleton.LocalClientId + " env�a su selecci�n del circuito: " + circuitoSeleccionado);

        recibirSeleccionCircuitoServerRpc(circuitoSeleccionado);
        opcionEnviada = true;
    }

    [ServerRpc(RequireOwnership = false)] // De esta forma se permite que cualquier cliente env�e mensajes al servidor
    void recibirSeleccionCircuitoServerRpc(int circuito, ServerRpcParams rpcParams = default)
    {
        Debug.Log("El servidor ha recibido la selecci�n del cliente " + rpcParams.Receive.SenderClientId + ": " + circuito);
        circuitosEscogidos[circuito]++;
        numSelecciones++;

        if (numSelecciones == TestLobby.Instance.NUM_PLAYERS_IN_LOBBY)
        {
            int mejorCircuito = 0;
            // Se busca la posici�n con m�s elegidos
            for (int i = 0; i < 4; i++)
            {
                if (circuitosEscogidos[i] > circuitosEscogidos[mejorCircuito])
                {
                    mejorCircuito = i;
                }
            }

            Debug.Log("Server selecting final circuit: " + mejorCircuito);
            // Dependiendo del circuito escogido, se asocia una lista de checkpoints u otra
            switch (mejorCircuito)
            {
                case 0:
                    CheckPointManager.instance.checkPoints = CheckPointManager.instance.checkPoints1;
                    break;
                case 1:
                    CheckPointManager.instance.checkPoints = CheckPointManager.instance.checkPoints2;
                    break;
                case 2:
                    CheckPointManager.instance.checkPoints = CheckPointManager.instance.checkPoints3;
                    break;
                case 3:
                    CheckPointManager.instance.checkPoints = CheckPointManager.instance.checkPoints4;
                    break;
            }

            CheckPointManager.instance.TotalCheckPoints = CheckPointManager.instance.checkPoints.Count;

            circuitos[mejorCircuito].SetActive(true);
            canvas.SetActive(false);
            circuitoSeleccionado = circuito;
            seleccionarCircuitoFinalClientRpc(mejorCircuito);
        }
    }

    [ClientRpc]
    void seleccionarCircuitoFinalClientRpc(int circuito)
    {
        Debug.Log("Client " + NetworkManager.Singleton.LocalClientId + " activating circuit: " + circuito);
        circuitos[circuito].SetActive(true);
        finalizarMenu = true;
        circuitoSeleccionado = circuito;
        // Dependiendo del circuito escogido, se asocia una lista de checkpoints u otra
        switch (circuito)
        {
            case 0:
                CheckPointManager.instance.checkPoints = CheckPointManager.instance.checkPoints1;
                break;
            case 1:
                CheckPointManager.instance.checkPoints = CheckPointManager.instance.checkPoints2;
                break;
            case 2:
                CheckPointManager.instance.checkPoints = CheckPointManager.instance.checkPoints3;
                break;
            case 3:
                CheckPointManager.instance.checkPoints = CheckPointManager.instance.checkPoints4;
                break;
        }

        CheckPointManager.instance.TotalCheckPoints = CheckPointManager.instance.checkPoints.Count;
        StartCoroutine(StartingSequence());

    }

    public void MostrarMenu()
    {
        canvasLobbyWaiting.SetActive(false);
        canvas.SetActive(true);
    }

    public void OcultarMenu()
    {
        // Antes de ocultar el men�, se spawnean los coches
        NetManager.instance.circuitoSeleccionado = circuitoSeleccionado;
        canvas.SetActive(false);
    }

    private IEnumerator StartingSequence()
    {
        // Fundido a negro
        yield return StartCoroutine(FadeController.instance.FadeOut());

        // Se generan los clientes (players)
        NetManager.instance.GeneratePlayersInCircuit();

        // Esperar dos segundos en el fundido
        yield return new WaitForSeconds(2f);
        // Si se est� reiniciando la carrera, se vuelven a modificar los colores de los coches en funci�n de lo escogido por cada jugador
        if(reiniciarCarrera)
        {
            ResetearCircuitoColores();
        }
        // En la vuelta de clasificaci�n, no se van a tener en cuenta las colisiones entre los coches. Esto se habilitar� despu�s, una vez empiece la carrera en s�
        NetManager.instance.IgnorarColisionesEntreJugadores(true);
        // Fundido desde negro
        yield return StartCoroutine(FadeController.instance.FadeIn());

    }

    public void ResetState()
    {
        botonAceptar.SetActive(false); // Hasta que no se seleccione un nuevo circuito no se puede continuar
        textoEspera.SetActive(false);
        opcionEnviada = false;
        finalizarMenu = false;
        // Se ocultan todos los circuitos
        for (int i = 0; i < 4; i++)
        {
            circuitos[i].SetActive(false);
        }
        // Se repite el proceso de selecci�n de circuito
        numSelecciones = 0;
        // Se reinician los votos
        for (int i = 0; i < 4; i++)
        {
            circuitosEscogidos[i] = 0;
        }
        EndingController.Instance.carreraFinalizada = false;
        EndingController.Instance.ordenFinal.Clear();
        if (!RaceController.instance.clasificacion)
        {
            UI_HUD.Instance.ResetState();
        }
        UI_Clasificacion.instance.ResetState();
        reiniciarCarrera = true;
    }

    private void ResetearCircuitoColores()
    {
        // Se resetea tambi�n el estado de la carrera
        RaceController.instance.ModificarColorCoches();
        RaceController.instance._circuitController.StartCircuit();
        reiniciarCarrera = false;
    }

}
