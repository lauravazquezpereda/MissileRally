using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class UI_Circuit : NetworkBehaviour
{
    // Se hace que esta clase sea un Singleton
    public static UI_Circuit instance;
    // Variable que va a almacenar el �ndice del circuito en el que se pinche, ya que son botones
    private int circuitoSeleccionado;
    // Bot�n de aceptar, que aparecer� cuando se haya seleccionado un circuito
    [SerializeField] GameObject botonAceptar;
    // Canvas de la pantalla
    [SerializeField] GameObject canvas;
    // Canvas de la pantalla anterior, para poder ocultarlo
    [SerializeField] GameObject canvasLobbyWaiting;
    // Texto de espera, en el caso de queden jugadores por seleccionar el circuito
    [SerializeField] GameObject textoEspera;

    // Variables que s�lo va a almacenar el servidor. Se deben proteger con exclusi�n mutua, para evitar que se puedan modificar al mismo tiempo y
    // generar problemas de concurrencia
    private int[] circuitosEscogidos;
    private int numSelecciones = 0;
    Object cerrojoCircuito = new Object(); // Cerrojo para garantizar la exclusi�n mutua

    // Lista de modelos de los circuitos, para activar el que se haya escogido
    public GameObject[] circuitos;
    // Variable que indica que se ha enviado el circuito escogido
    private bool opcionEnviada = false;
    // Variable que se hace que se oculte el men� una vez se haya terminado el proceso de selecci�n
    public bool finalizarMenu = false;
    // Variable que indica que se est� reiniciando el men� tras haber hecho ya una carrera
    private bool reiniciarCarrera = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    // Se inicializa la lista de circuitos con los contadores
    private void Start()
    {
        circuitosEscogidos = new int[4];
    }

    private void Update()
    {
        // Si se ha finalizado, se oculta el men�
        if(finalizarMenu)
        {
            OcultarMenu();
            finalizarMenu = false;
        }
    }
    // Esta funci�n se ejecuta al pulsar sobre cada bot�n, cada uno con un �ndice distinto
    public void escogerCircuito(int numCircuito)
    {
        botonAceptar.SetActive(true);
        circuitoSeleccionado = numCircuito;
    }
    // Esta funci�n se ejecuta cuando se pulsa el bot�n de aceptar tras seleccionar el circuito
    public void aceptarSeleccion()
    {
        // Solo se puede enviar una selecci�n, para no confundir al servidor
        if (opcionEnviada) return;

        // Asegurarse de que este c�digo se ejecuta en el cliente
        if (!IsClient) return;

        // Se muestra un texto que indica que hay que esperar a que todos los jugadores escojan
        textoEspera.SetActive(true);
        Debug.Log("Cliente " + NetworkManager.Singleton.LocalClientId + " env�a su selecci�n del circuito: " + circuitoSeleccionado);

        // Se ejecuta la l�gica de suma del circuito seleccionado en el servidor
        recibirSeleccionCircuitoServerRpc(circuitoSeleccionado);

        opcionEnviada = true;
    }

    [ServerRpc(RequireOwnership = false)] // De esta forma se permite que cualquier cliente env�e mensajes al servidor
    void recibirSeleccionCircuitoServerRpc(int circuito, ServerRpcParams rpcParams = default)
    {
        Debug.Log("El servidor ha recibido la selecci�n del cliente " + rpcParams.Receive.SenderClientId + ": " + circuito);
        // El proceso de actualizar la lista de contadores de cu�ntos jugadores han escogido cada circuito se debe proteger bajo exclusi�n mutua, para evitar que se solapen
        lock(cerrojoCircuito)
        {
            circuitosEscogidos[circuito]++;
            numSelecciones++;
        }
        // Si se han recibido tantas selecciones como jugadores hay en el lobby, se procede a buscar cu�l tiene m�s votos
        if (numSelecciones == TestLobby.Instance.NUM_PLAYERS_IN_LOBBY)
        {
            int mejorCircuito = 0;
            // Se busca la posici�n con m�s elegidos, con un algoritmo de b�squeda del mejor candidato sencillo
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
            // Se indica el total de checkpoints que hay
            CheckPointManager.instance.TotalCheckPoints = CheckPointManager.instance.checkPoints.Count;
            // Se activa el circuito que se ha escogido
            circuitos[mejorCircuito].SetActive(true);
            // Se desactiva el canvas
            canvas.SetActive(false);
            // Se indica cu�l es el circuito seleccionado
            circuitoSeleccionado = circuito;
            // Se comunica el resultado a los clientes
            seleccionarCircuitoFinalClientRpc(mejorCircuito);
        }
    }

    [ClientRpc]
    void seleccionarCircuitoFinalClientRpc(int circuito)
    {
        Debug.Log("Client " + NetworkManager.Singleton.LocalClientId + " activating circuit: " + circuito);
        // Se activa el circuito seg�n lo recibido del servidor
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
        // Se comienza la secuencia de inicio de la carrera
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
    // Esta funci�n se ejecuta cuando tras haber empezado ya una carrera, despu�s se quiere iniciar otra
    public void ResetState()
    {
        // Se reinician todas las variables a seg�n estaban por defecto
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
        // Se limpia el orden de los jugadores que hayan llegado a meta
        EndingController.Instance.carreraFinalizada = false;
        EndingController.Instance.corredoresLlegados = 0; // Se reinician los corredores que han llegado a meta
        EndingController.Instance.ordenFinal.Clear();
        // Se resetea tambi�n el estado del HUD la carrera
        if (!RaceController.instance.clasificacion)
        {
            UI_HUD.Instance.ResetState();
        }
        // Y tambi�n de la clasificaci�n
        UI_Clasificacion.instance.ResetState();
        reiniciarCarrera = true;
    }

    private void ResetearCircuitoColores()
    {
        // Se vuelven a modificar los colores de los coches
        RaceController.instance.ModificarColorCoches();
        // Se resetea el controlador del circuito de nuevo
        RaceController.instance._circuitController.StartCircuit();
        reiniciarCarrera = false;
    }

}
