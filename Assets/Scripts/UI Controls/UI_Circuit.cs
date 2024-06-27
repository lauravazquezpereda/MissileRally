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

    // Variables que sólo va a almacenar el servidor
    private int[] circuitosEscogidos;
    private int numSelecciones = 0;

    // Lista de circuitos
    public GameObject[] circuitos;

    private bool opcionEnviada = false;
    public bool finalizarMenu = false;

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

        // Asegurarse de que este código se ejecuta en el cliente
        if (!IsClient) return;

        textoEspera.SetActive(true);
        Debug.Log("Cliente " + NetworkManager.Singleton.LocalClientId + " envía su selección del circuito: " + circuitoSeleccionado);

        recibirSeleccionCircuitoServerRpc(circuitoSeleccionado);
        opcionEnviada = true;
    }

    [ServerRpc(RequireOwnership = false)] // De esta forma se permite que cualquier cliente envíe mensajes al servidor
    void recibirSeleccionCircuitoServerRpc(int circuito, ServerRpcParams rpcParams = default)
    {
        Debug.Log("El servidor ha recibido la selección del cliente " + rpcParams.Receive.SenderClientId + ": " + circuito);
        circuitosEscogidos[circuito]++;
        numSelecciones++;

        if (numSelecciones == TestLobby.Instance.NUM_PLAYERS_IN_LOBBY)
        {
            int mejorCircuito = 0;
            // Se busca la posición con más elegidos
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
    }

    public void MostrarMenu()
    {
        canvasLobbyWaiting.SetActive(false);
        canvas.SetActive(true);
    }

    public void OcultarMenu()
    {
        // Antes de ocultar el menú, se spawnean los coches
        NetManager.instance.circuitoSeleccionado = circuitoSeleccionado;
        NetManager.instance.GeneratePlayersInCircuit();
        canvas.SetActive(false);
    }

}
