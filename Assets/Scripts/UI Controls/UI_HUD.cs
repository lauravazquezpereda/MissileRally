using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class UI_HUD : NetworkBehaviour
{
    // Se hace que esta clase sea un Singleton
    public static UI_HUD Instance;

    // Estas variables se utilizan para calcular la orientaci�n de la aguja del veloc�metro en funci�n de la velocidad del coche
    private const float ANGULO_MINIMO = 83.0f;
    private const float ANGULO_MAXIMO = -142.0F;
    private const float MAX_VELOCIDAD = 55f;
    // Referencia a la imagen del veloc�metro
    [SerializeField] GameObject agujaVelocimetro;
    // Texto que muestra en qu� vuelta se encuentra el jugador
    [SerializeField] TMP_Text numeroVuelta;
    // Texto que muestra cu�nto tiempo lleva el jugador en la carrera
    [SerializeField] TMP_Text tiempoTotal;
    // Texto que muestra el tiempo que lleva el jugador en la vuelta actual
    [SerializeField] TMP_Text tiempoVuelta;

    public float tTotal; // Contador de tiempo total
    public float tVuelta; // Contador de tiempo por vuelta

    public bool inicioCarrera = false; // Se indica si se ha inicializado la carrera
    public bool vueltasInicializadas = false; // Se indica si se han inicializado las listas de vueltas

    // Se almacenan las vueltas de los jugadores en el servidor
    public int[] vueltasJugadores; // N�mero de vueltas que lleva cada jugador
    public float[] tiemposVueltaJugadores; // Tiempo que ha tardado cada jugador en dar cada vuelta

    [SerializeField] private GameObject canvasTiemposFinal; // Pantalla que muestra los resultados finales

    [SerializeField] private GameObject[] elementoHUD; // Lista con todos los elementos del HUD para poder ocultarlos
    [SerializeField] private GameObject textoEsperaFinal; // Texto de espera para mostrar cuando un jugador ha terminado la carrera y queda alguno m�s por terminar

    // Resultados finales
    [SerializeField] private TMP_Text[] clasificacion; // Clasificaci�n del final de carrera
    [SerializeField] private TMP_Text[] tiemposFinalesVuelta; // Tiempos finales de cada vuelta

    // Posici�n de carrera
    [SerializeField] private TMP_Text posicionCarrera; // Muestra la posici�n del coche en la carrera


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Se prepara la carrera
    public void PrepareRace()
    {
        inicioCarrera = true;
        StartCoroutine(StartRace());
    }

    private void Update()
    {
        if (!inicioCarrera) return;

        if(!vueltasInicializadas)
        {
            // Se inicializa la lista de vueltas
            vueltasInicializadas = true;
            if (IsServer)
            {
                // Inicializar el array de vueltas
                vueltasJugadores = new int[TestLobby.Instance.NUM_PLAYERS_IN_LOBBY];
                for (int i = 0; i < TestLobby.Instance.NUM_PLAYERS_IN_LOBBY; i++)
                {
                    vueltasJugadores[i] = 0;
                }

                // Inicializar el array de los tiempos de cada vuelta, cada jugador dar� 3 vueltas
                tiemposVueltaJugadores = new float[TestLobby.Instance.NUM_PLAYERS_IN_LOBBY * 3];
                for (int i = 0; i < TestLobby.Instance.NUM_PLAYERS_IN_LOBBY * 3; i++)
                {
                    tiemposVueltaJugadores[i] = 0;
                }
            }
        }
        // Continuamente se actualizan los temporizadores con el tiempo total y el tiempo por vuelta
        tTotal += Time.deltaTime;
        tVuelta += Time.deltaTime;
        // Adem�s, se actualizan los textos
        ActualizarTemporizador(tiempoTotal, tTotal);
        ActualizarTemporizador(tiempoVuelta, tVuelta);
        // Tambi�n, se actualiza la posici�n del coche en la carrera
        ActualizarPosicionCarrera();

    }

    public void ModificarVelocimetro(float velocidad)
    {
        Vector3 rotacionActual = agujaVelocimetro.transform.rotation.eulerAngles;

        // Se interpola el valor de la rotaci�n en funci�n de la velocidad actual
        float rotacionVelocidad_Z = Mathf.Lerp(ANGULO_MINIMO, ANGULO_MAXIMO, velocidad / MAX_VELOCIDAD);
        agujaVelocimetro.transform.rotation = Quaternion.Euler(new Vector3(rotacionActual.x, rotacionActual.y, rotacionVelocidad_Z));     
    }


    public void AvanzarVuelta()
    {
        // Cada vez que se pasa por la meta siendo propietario, se ejecuta este m�todo y se comunica al servidor
        int playerIndex = (int)NetworkManager.Singleton.LocalClientId;
        AvanzarVueltaServerRpc(playerIndex, tVuelta);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AvanzarVueltaServerRpc(int playerIndex, float tiempoVuelta)
    {
        Debug.Log("Avanzar vuelta");
        // Se obtiene en qu� vuelta se encuentra el jugador justo antes de pasar por la meta
        int vueltaActual = vueltasJugadores[playerIndex] - 1;
        if(vueltaActual >= 0)
        {
            // Se a�ade el tiempo en la lista de tiempos (est�n ordenadas las 3 vueltas de cada jugador del 0 al 3). Por ello se multiplica por el n�mero de vueltas (3) y el �ndice,
            // sum�ndole la vuelta actual
            tiemposVueltaJugadores[3 * playerIndex + vueltaActual] = tiempoVuelta;
        }
        // Se actualiza el n�mero de vueltas s�lo en el servidor
        vueltasJugadores[playerIndex]++;
        // No es necesario proteger estas variables, ya que, cada cliente modifica las suyas y no se solapan
        if (vueltasJugadores[playerIndex] <= 3)
        {
            // Si la vuelta a la que se avanza no indica que es el final, se comunica al cliente
            ActualizarNumeroVueltasClientRpc(vueltasJugadores[playerIndex], playerIndex);
        }
        else
        {
            // Si no, se ejecuta el final de carrera en el cliente
            FinalizarCarreraClientRpc(playerIndex);
        }
    }

    [ClientRpc]
    private void ActualizarNumeroVueltasClientRpc(int vueltaActual, int playerIndex)
    {
        // Se actualiza el n�mero de vuelta del jugador correspondiente en la lista de jugadores, para poder actualizar bien las posiciones de carrera
        foreach(PlayerNetwork player in RaceController.instance._players)
        {
            if(player.ID == playerIndex)
            {
                player.CurrentLap = vueltaActual;
            }
        }
        if (playerIndex != (int)NetworkManager.Singleton.LocalClientId) return;
        // S�lo se actualiza el n�mero de vuelta en el propietario, el resto s�lo sirven para procesar las posiciones
        numeroVuelta.text = vueltaActual.ToString() + "/3";
        if (vueltaActual - 1 > 0)
        {
            // Se reinicia el tiempo por vuelta
            Debug.Log("Reiniciando tiempo");
            tVuelta = 0;
        }
    }

    [ClientRpc]
    private void FinalizarCarreraClientRpc(int playerIndex)
    {
        if (playerIndex != (int)NetworkManager.Singleton.LocalClientId) return;
        // Una vez el corredor llega a la meta, se avisa al controlador, que se ejecuta en el servidor
        Debug.Log("He terminado la carrera");
        EndingController.Instance.carreraFinalizada = true;
        // Se ocultan todos los elementos del HUD
        for(int i = 0; i < elementoHUD.Length; i++)
        {
            elementoHUD[i].SetActive(false);
        }
        textoEsperaFinal.SetActive(true);
        // Se avisa al controlador de la finalizaci�n en el servidor, para que guarde los tiempos y vea si ya han terminado todos los jugadores necesarios
        EndingController.Instance.AvisarMetaServerRpc(playerIndex);
        
    }

    [ClientRpc]
    public void MostrarResultadosCarreraClientRpc(int pos1, int pos2, int pos3, int pos4, FloatArray tiemposVueltaJugador)
    {
        Debug.Log("Mostrando resultados");

        canvasTiemposFinal.SetActive(true);

        // Cada cliente va a mostrar la clasificaci�n final
        // Se obtienen los nombres de los jugadores presentes, para mostrarlo en la clasificaci�n
        Dictionary<string, List<string>> datosJugadores = TestLobby.Instance.GetPlayersInLobby();
        datosJugadores.TryGetValue("Nombres", out List<string> nombres);

        // Para cada posici�n, primero se comprueba que su valor sea distinto a -1, lo que quiere decir que hay alg�n coche ocupando dicha posici�n
        // Despu�s, mediante el array serializado de los tiempos por vuelta, se calcula el tiempo total
        // Si el jugador queda en �ltima posici�n, es decir, la carrera ha finalizado sin que termine alguna vuelta, no se muestra su tiempo, ya que no ha completado el recorrido

        if (pos1 != -1)
        {
            float tiempoTotal = 0;
            // Teniendo en cuenta c�mo est�n ordenados los tiempos por vuelta, se calcula el tiempo total de carrera de cada jugador
            for(int i = pos1*3; i < pos1*3 + 3; i++)
            {
                tiempoTotal += tiemposVueltaJugador.Values[i];
            }

            int minutes = Mathf.FloorToInt(tiempoTotal / 60);
            int seconds = Mathf.FloorToInt(tiempoTotal % 60);
            // Se muestra por pantalla
            clasificacion[0].text = "1� - " + nombres[pos1] + " -> "+ string.Format("{0:00}:{1:00}", minutes, seconds); 
            Debug.Log(pos1);
        }
        if(pos2 != -1)
        {
            float tiempoTotal = 0;
            for (int i = pos2*3; i < pos2*3 + 3; i++)
            {
                tiempoTotal += tiemposVueltaJugador.Values[i];
                // En caso de que alguna vuelta valga 0 segundos, quiere decir que el jugador no ha terminado la carrera, por lo que, no se muestra un tiempo total
                if (tiemposVueltaJugador.Values[i] == 0)
                {
                    tiempoTotal = 0;
                    break;
                }
            }
            if(tiempoTotal == 0)
            {
                clasificacion[1].text = "2� - " + nombres[pos2] + " -> -----------";
            }
            else
            {
                int minutes = Mathf.FloorToInt(tiempoTotal / 60);
                int seconds = Mathf.FloorToInt(tiempoTotal % 60);
                clasificacion[1].text = "2� - " + nombres[pos2] + " -> " + string.Format("{0:00}:{1:00}", minutes, seconds);
            }
            Debug.Log(pos2);
        }
        if(pos3 != -1)
        {
            float tiempoTotal = 0;
            for (int i = pos3*3; i < pos3*3 + 3; i++)
            {
                tiempoTotal += tiemposVueltaJugador.Values[i];
                if (tiemposVueltaJugador.Values[i] == 0)
                {
                    tiempoTotal = 0;
                    break;
                }
            }
            if (tiempoTotal == 0)
            {
                clasificacion[2].text = "3� - " + nombres[pos3] + " -> -----------";
            }
            else
            {
                int minutes = Mathf.FloorToInt(tiempoTotal / 60);
                int seconds = Mathf.FloorToInt(tiempoTotal % 60);
                clasificacion[2].text = "3� - " + nombres[pos3] + " -> " + string.Format("{0:00}:{1:00}", minutes, seconds);
            }
            Debug.Log(pos2);
        }
        // Si hay alg�n coche en cuarta posici�n, obligatoriamente no ha terminado la carrera
        if(pos4 != -1)
        {
            clasificacion[3].text = "4� - " + nombres[pos4] + " -> -----------";
            Debug.Log(pos4);
        }

        // Despu�s de mostrar la clasificaci�n, se muestran los tiempos por vuelta de cada jugador. Por ello, hay que realizar una verificaci�n de qu� cliente
        // es el que recibe el mensaje

        int jugador = (int)NetworkManager.Singleton.LocalClientId;

        // Se debe tener en cuenta que los tiempos por vuelta est�n ordenados uno detr�s de otro. Por ello, se empieza a recorrer dependiendo del id del jugador
        int numVuelta = 0; // Contador para mostrar el texto
        for(int i = jugador * 3; i < jugador * 3 + 3; i++)
        {           
            float tiempoVuelta = tiemposVueltaJugador.Values[i];
            // Si el tiempo es 0, quiere decir que el jugador no ha completado la vuelta, por lo que no se registra un tiempo
            if(tiempoVuelta == 0)
            {
                tiemposFinalesVuelta[numVuelta].text = numVuelta.ToString() + " -   -----------";
            }
            else
            {
                int minutes = Mathf.FloorToInt(tiempoVuelta / 60);
                int seconds = Mathf.FloorToInt(tiempoVuelta % 60);
                tiemposFinalesVuelta[numVuelta].text = numVuelta.ToString() + " -   " + string.Format("{0:00}:{1:00}", minutes, seconds);
            }
            numVuelta++;
        }

        // Se eliminan los coches
        EliminarCoches();

        // Se resetea el dato de que el host ha comenzado la partida
        TestLobby.Instance.ReiniciarEsperaHost();

    }
    // Esta funci�n actualiza el texto del temporizador
    private void ActualizarTemporizador(TMP_Text texto, float tiempo)
    {
        int minutes = Mathf.FloorToInt(tiempo / 60F);
        int seconds = Mathf.FloorToInt(tiempo % 60F);
        texto.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    // Esta funci�n se encarga de actualizar la posici�n de la carrera en funci�n de la lista ordenada de jugadores
    private void ActualizarPosicionCarrera()
    {
        int posicion = 0;
        foreach(PlayerNetwork player in RaceController.instance._players)
        {
            posicion++;
            if(player.IsOwner)
            {
                posicionCarrera.text = posicion.ToString() + "�";
                return;
            }
        }
    }

    private void EliminarCoches()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // Se obtienen todos los objetos player de la escena, para eliminarlos
            GameObject[] carPlayers = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject carPlayer in carPlayers)
            {
                if (carPlayer != null && carPlayer.GetComponent<NetworkObject>().IsSpawned)
                {
                    carPlayer.GetComponent<NetworkObject>().Despawn(true); // True para destruir el objeto en todas las instancias
                }
            }
        }

        // Se limpia la lista
        RaceController.instance._players.Clear();
        RaceController.instance.numPlayers = 0;
    }

    // Esta funci�n se ejecuta para limpiar el estado de la clase, en el caso de que se quiera hacer una nueva carrera
    public void ResetState()
    {
        // Se indica que no se ha iniciado la carrera, que las vueltas est�n sin inicializar
        inicioCarrera = false;
        vueltasInicializadas = false;
        // Se vuelven a activar todos los elementos del HUD y a desactivar el texto de que se ha llegado a la meta
        for (int i = 0; i < elementoHUD.Length; i++)
        {
            elementoHUD[i].SetActive(true);
        }
        textoEsperaFinal.SetActive(false);
        // Se reinician los temporizadores y el n�mero de vueltas por defecto
        tTotal = 0f;
        tVuelta = 0f;
        ActualizarTemporizador(tiempoTotal, tTotal);
        ActualizarTemporizador(tiempoVuelta, tVuelta);
        numeroVuelta.text = "1/3";

    }

    private IEnumerator StartRace()
    {
        // Fundido a negro
        yield return StartCoroutine(FadeController.instance.FadeOut());

        // Se colocan los jugadores en su posici�n correcta en funci�n del resultado de la clasificaci�n
        NetManager.instance.GeneratePlayersOrderedInCircuit();

        // Esperar dos segundos en el fundido
        yield return new WaitForSeconds(2f);

        // Se vuelven a establecer los colores
        RaceController.instance.ModificarColorCoches();

        // Fundido desde negro
        yield return StartCoroutine(FadeController.instance.FadeIn());

        ReinicializarVueltasServerRpc();

    }

    // Se reinician las vueltas, en caso de que al colocar los players, alguno haya travesado la l�nea de meta
    [ServerRpc(RequireOwnership = false)]
    private void ReinicializarVueltasServerRpc()
    {
        // Inicializar el array de vueltas
        vueltasJugadores = new int[TestLobby.Instance.NUM_PLAYERS_IN_LOBBY];
        for (int i = 0; i < TestLobby.Instance.NUM_PLAYERS_IN_LOBBY; i++)
        {
            vueltasJugadores[i] = 0;
        }

        // Inicializar el array de los tiempos de cada vuelta, cada jugador dar� 3 vueltas
        tiemposVueltaJugadores = new float[TestLobby.Instance.NUM_PLAYERS_IN_LOBBY * 3];
        for (int i = 0; i < TestLobby.Instance.NUM_PLAYERS_IN_LOBBY * 3; i++)
        {
            tiemposVueltaJugadores[i] = 0;
        }
    }

}
