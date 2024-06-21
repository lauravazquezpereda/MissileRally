using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class UI_HUD : NetworkBehaviour
{
    public static UI_HUD Instance;

    private const float ANGULO_MINIMO = 83.0f;
    private const float ANGULO_MAXIMO = -142.0F;
    private const float MAX_VELOCIDAD = 55f;

    [SerializeField] GameObject agujaVelocimetro;

    [SerializeField] TMP_Text numeroVuelta;

    [SerializeField] TMP_Text tiempoTotal;
    [SerializeField] TMP_Text tiempoVuelta;

    private float tTotal;
    private float tVuelta;

    public bool inicioCarrera = false;
    private bool vueltasInicializadas = false;

    // Se almacenan las vueltas de los jugadores en el servidor
    [SerializeField] private int[] vueltasJugadores;
    public float[] tiemposVueltaJugadores;

    [SerializeField] private GameObject canvasTiemposFinal;

    [SerializeField] private GameObject[] elementoHUD;
    [SerializeField] private GameObject textoEsperaFinal;

    // Resultados finales
    [SerializeField] private TMP_Text[] clasificacion;
    [SerializeField] private TMP_Text[] tiemposFinalesVuelta;

    [SerializeField] GameObject canvasLobby;


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

    private void Update()
    {
        if (!inicioCarrera) return;

        if(!vueltasInicializadas)
        {
            EndingController.Instance.PrepararFinalServerRpc(TestLobby.Instance.NUM_PLAYERS_IN_LOBBY);
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

                // Inicializar el array de los tiempos de cada vuelta, cada jugador dará 3 vueltas
                tiemposVueltaJugadores = new float[TestLobby.Instance.NUM_PLAYERS_IN_LOBBY * 3];
                for (int i = 0; i < TestLobby.Instance.NUM_PLAYERS_IN_LOBBY * 3; i++)
                {
                    tiemposVueltaJugadores[i] = 0;
                }
            }
        }

        tTotal += Time.deltaTime;
        tVuelta += Time.deltaTime;

        ActualizarTemporizador(tiempoTotal, tTotal);
        ActualizarTemporizador(tiempoVuelta, tVuelta);

    }

    public void ModificarVelocimetro(float velocidad)
    {
        Vector3 rotacionActual = agujaVelocimetro.transform.rotation.eulerAngles;

        // Se interpola el valor de la rotación en función de la velocidad actual
        float rotacionVelocidad_Z = Mathf.Lerp(ANGULO_MINIMO, ANGULO_MAXIMO, velocidad / MAX_VELOCIDAD);
        agujaVelocimetro.transform.rotation = Quaternion.Euler(new Vector3(rotacionActual.x, rotacionActual.y, rotacionVelocidad_Z));     
    }


    public void AvanzarVuelta()
    {
        int playerIndex = (int)NetworkManager.Singleton.LocalClientId;
        AvanzarVueltaServerRpc(playerIndex, tVuelta);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AvanzarVueltaServerRpc(int playerIndex, float tiempoVuelta)
    {
        Debug.Log("Avanzar vuelta");
        // Se añade el tiempo de vuelta
        int vueltaActual = vueltasJugadores[playerIndex] - 1;
        if(vueltaActual >= 0)
        {
            tiemposVueltaJugadores[3 * playerIndex + vueltaActual] = tiempoVuelta;
        }
        // Se actualiza el número de vueltas sólo en el servidor
        vueltasJugadores[playerIndex]++;
        if (vueltasJugadores[playerIndex] <= 3)
        {
            ActualizarNumeroVueltasClientRpc(vueltasJugadores[playerIndex], playerIndex);
        }
        else
        {
            FinalizarCarreraClientRpc(playerIndex);
        }
    }

    [ClientRpc]
    private void ActualizarNumeroVueltasClientRpc(int vueltaActual, int playerIndex)
    {
        if (playerIndex != (int)NetworkManager.Singleton.LocalClientId) return;
        numeroVuelta.text = vueltaActual.ToString() + "/3";
        if (vueltaActual - 1 > 0)
        {
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
        EndingController.Instance.AvisarMetaServerRpc(playerIndex);
        
    }

    [ClientRpc]
    public void MostrarResultadosCarreraClientRpc(int pos1, int pos2, int pos3, int pos4, FloatArray tiemposVueltaJugador)
    {
        Debug.Log("Mostrando resultados");

        canvasTiemposFinal.SetActive(true);

        // Cada cliente va a mostrar la clasificación final
        // Se obtienen los nombres de los jugadores presentes, para mostrarlo en la clasificación
        Dictionary<string, List<string>> datosJugadores = TestLobby.Instance.GetPlayersInLobby();
        datosJugadores.TryGetValue("Nombres", out List<string> nombres);

        // Para cada posición, primero se comprueba que su valor sea distinto a -1, lo que quiere decir que hay algún coche ocupando dicha posición
        // Después, mediante el array serializado de los tiempos por vuelta, se calcula el tiempo total
        // Si el jugador queda en última posición, es decir, la carrera ha finalizado sin que termine alguna vuelta, no se muestra su tiempo, ya que no ha completado el recorrido

        if (pos1 != -1)
        {
            float tiempoTotal = 0;
            for(int i = pos1; i < pos1 + 3; i++)
            {
                tiempoTotal += tiemposVueltaJugador.Values[i];
            }

            int minutes = Mathf.FloorToInt(tiempoTotal / 60);
            int seconds = Mathf.FloorToInt(tiempoTotal % 60);

            clasificacion[0].text = "1º - " + nombres[pos1] + " -> "+ string.Format("{0:00}:{1:00}", minutes, seconds); 
            Debug.Log(pos1);
        }
        if(pos2 != -1)
        {
            float tiempoTotal = 0;
            for (int i = pos2; i < pos2 + 3; i++)
            {
                tiempoTotal += tiemposVueltaJugador.Values[i];
                if (tiemposVueltaJugador.Values[i] == 0)
                {
                    tiempoTotal = 0;
                    break;
                }
            }
            if(tiempoTotal == 0)
            {
                clasificacion[1].text = "2º - " + nombres[pos2] + " -> -----------";
            }
            else
            {
                int minutes = Mathf.FloorToInt(tiempoTotal / 60);
                int seconds = Mathf.FloorToInt(tiempoTotal % 60);
                clasificacion[1].text = "2º - " + nombres[pos2] + " -> " + string.Format("{0:00}:{1:00}", minutes, seconds);
            }
            Debug.Log(pos2);
        }
        if(pos3 != -1)
        {
            float tiempoTotal = 0;
            for (int i = pos3; i < pos3 + 3; i++)
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
                clasificacion[2].text = "3º - " + nombres[pos3] + " -> -----------";
            }
            else
            {
                int minutes = Mathf.FloorToInt(tiempoTotal / 60);
                int seconds = Mathf.FloorToInt(tiempoTotal % 60);
                clasificacion[2].text = "3º - " + nombres[pos3] + " -> " + string.Format("{0:00}:{1:00}", minutes, seconds);
            }
            Debug.Log(pos2);
        }
        // Si hay algún coche en cuarta posición, obligatoriamente no ha terminado la carrera
        if(pos4 != -1)
        {
            clasificacion[3].text = "4º - " + nombres[pos4] + " -> -----------";
            Debug.Log(pos4);
        }

        // Después de mostrar la clasificación, se muestran los tiempos por vuelta de cada jugador. Por ello, hay que realizar una verificación de qué cliente
        // es el que recibe el mensaje

        int jugador = (int)NetworkManager.Singleton.LocalClientId;

        // Se debe tener en cuenta que los tiempos por vuelta están ordenados uno detrás de otro. Por ello, se empieza a recorrer dependiendo del id del jugador
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

    }

    public void VolverLobby()
    {
        canvasLobby.SetActive(true);
        canvasTiemposFinal.SetActive(false);
    }

    private void ActualizarTemporizador(TMP_Text texto, float tiempo)
    {
        int minutes = Mathf.FloorToInt(tiempo / 60F);
        int seconds = Mathf.FloorToInt(tiempo % 60F);
        texto.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

}
