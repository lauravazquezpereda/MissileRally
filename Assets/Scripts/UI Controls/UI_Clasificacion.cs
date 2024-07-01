using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UI_Clasificacion : NetworkBehaviour
{
    public static UI_Clasificacion instance;

    public bool enClasificacion = true;

    // Velocímetro
    private const float ANGULO_MINIMO = 83.0f;
    private const float ANGULO_MAXIMO = -142.0F;
    private const float MAX_VELOCIDAD = 55f;

    [SerializeField] GameObject agujaVelocimetro;

    // Tiempos de vuelta de clasificación
    // Estructura para almacenar los tiempos junto con el identificador
    public struct Clasificacion
    {
        public int id;
        public float tiempo;

        public Clasificacion(int id, float tiempo)
        {
            this.id = id;
            this.tiempo = tiempo;
        }
    }
    // Clase que implementa la interfaz para comparar los tiempos
    public class ComparerPorTiempo : Comparer<Clasificacion>
    {
        public override int Compare(Clasificacion x, Clasificacion y)
        {
            return x.tiempo.CompareTo(y.tiempo);
        }
    }

    public List<Clasificacion> tiemposClasificacion = new(4);
    public int numeroTerminados = 0;
    public float tVuelta = 0f;

    public TMP_Text[] tiemposVueltas;
    public int contadorTextos = 0;

    public bool inicioCarrera = false;
    private bool temporizadorActivo = false;

    public TMP_Text temporizador;

    // NECESARIO PARA DESPUÉS DE TERMINAR LA CLASIFICACIÓN
    public GameObject canvasCarrera;
    public GameObject fondoClasificacion;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    private void Update()
    {
        if(temporizadorActivo)
        {
            tVuelta += Time.deltaTime;
            ActualizarTemporizador(temporizador, tVuelta);
        }
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
        temporizadorActivo = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void AvanzarVueltaServerRpc(int playerIndex, float tiempoVuelta)
    {
        // Se añade el tiempo de vuelta
        Clasificacion vueltaClasificacion = new(playerIndex, tiempoVuelta);
        tiemposClasificacion.Add(vueltaClasificacion);

        // Se marca en el servidor que el coche ya está esperando al resto
        GameObject[] jugadores = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject jugador in jugadores)
        {
            PlayerNetwork identificadorJugador = jugador.GetComponent<PlayerNetwork>();
            if(identificadorJugador.ID == playerIndex)
            {
                CarController coche = jugador.GetComponentInChildren<CarController>();
                coche.esperandoClasificacion = true;
            }
        }

        // Se comunica el tiempo al resto de clientes para que lo pinten en su pantalla
        MostrarTiempoClientRpc(playerIndex, tiempoVuelta);

        numeroTerminados++;
        if(numeroTerminados == TestLobby.Instance.NUM_PLAYERS_IN_LOBBY)
        {
            // Quiere decir que todos han terminado la vuelta de clasificación
            PrepararCarrera();
        }
       
    }

    [ClientRpc]
    private void MostrarTiempoClientRpc(int playerIndex, float tiempoVuelta)
    {
        // Se obtienen los nombres de los jugadores presentes, para mostrarlo en la clasificación
        Dictionary<string, List<string>> datosJugadores = TestLobby.Instance.GetPlayersInLobby();
        datosJugadores.TryGetValue("Nombres", out List<string> nombres);

        string nombreJugador = nombres[playerIndex];

        int minutes = Mathf.FloorToInt(tiempoVuelta / 60);
        int seconds = Mathf.FloorToInt(tiempoVuelta % 60);


        tiemposVueltas[contadorTextos].text = nombreJugador + "- " + string.Format("{0:00}:{1:00}", minutes, seconds);
        contadorTextos++;
    }

    private void PrepararCarrera()
    {
        // Se ordenan los tiempos, para colocar los coches en la salida
        tiemposClasificacion.Sort(new ComparerPorTiempo());
        Debug.Log("Clasificación terminada");
        // Se obtiene una lista con los índices de los jugadores en función de su tiempo de clasificación
        List<int> posicionesClasificacion = new List<int>();
        foreach(Clasificacion c in tiemposClasificacion)
        {
            posicionesClasificacion.Add(c.id);
        }
        canvasCarrera.SetActive(true);
        fondoClasificacion.SetActive(false);
        // Se eliminan los coches, para generarlos de nuevo en su lugar
        EliminarCoches();
        // Se comunica esa lista a la clase NetManager, para que pueda colocar los coches en su posición correcta de salida
        NetManager.instance.posicionesClasificacion = posicionesClasificacion;
        // Se serializa la lista de posiciones
        IntList tiemposVuelta = new IntList { Values = posicionesClasificacion };
        // Se indica a la carrera que ya ha terminado la clasificación
        RaceController.instance.clasificacion = false;
        PrepararCarreraClientRpc(tiemposVuelta);
    }

    [ClientRpc]
    private void PrepararCarreraClientRpc(IntList tiemposVuelta)
    {
        // Se asegura de limpiar la lista de jugadores
        RaceController.instance._players.Clear();
        RaceController.instance.numPlayers = 0;
        // Se deserializa la lista
        List<int> posiciones = new List<int>();
        for(int i = 0; i< tiemposVuelta.Values.Count; i++)
        {
            posiciones.Add(tiemposVuelta.Values[i]);
        }
        // Se comunica esa lista a la clase NetManager, para que pueda colocar los coches en su posición correcta de salida
        NetManager.instance.posicionesClasificacion = posiciones;
        // Se indica a la carrera que ya ha terminado la clasificación
        RaceController.instance.clasificacion = false;
        // Se muestra el canvas en el cliente
        canvasCarrera.SetActive(true);
        // Se prepara el HUD
        UI_HUD.Instance.PrepareRace();
        fondoClasificacion.SetActive(false);
    }

    public void EmpezarTemporizador()
    {
        temporizadorActivo = true;
    }

    private void ActualizarTemporizador(TMP_Text texto, float tiempo)
    {
        int minutes = Mathf.FloorToInt(tiempo / 60F);
        int seconds = Mathf.FloorToInt(tiempo % 60F);
        texto.text = string.Format("{0:00}:{1:00}", minutes, seconds);
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

    // Con esta función se vuelve a tener el estado como al principio, por si se quiere echar otra carrera
    public void ResetState()
    {
        // Se indica que se vuelve a estar en proceso de clasificación
        enClasificacion = true;
        RaceController.instance.clasificacion = true;
        // Se limpian los tiempos anteriores 
        tiemposClasificacion.Clear();
        numeroTerminados = 0;
        tVuelta = 0;
        // Se restauran los textos con los tiempos de todos los jugadores
        foreach(TMP_Text texto in tiemposVueltas)
        {
            texto.text = "";
        }
        contadorTextos = 0;
        temporizadorActivo = false;
        temporizador.text = "0:00";
        inicioCarrera = false;
    }
}
