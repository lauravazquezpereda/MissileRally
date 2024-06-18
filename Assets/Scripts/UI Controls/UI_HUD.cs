using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_HUD : MonoBehaviour
{
    public static UI_HUD Instance;

    private const float ANGULO_MINIMO = 83.0f;
    private const float ANGULO_MAXIMO = -142.0F;
    private const float MAX_VELOCIDAD = 55f;

    [SerializeField] GameObject agujaVelocimetro;

    [SerializeField] int vueltaActual = 0;
    [SerializeField] TMP_Text numeroVuelta;

    [SerializeField] TMP_Text tiempoTotal;
    [SerializeField] TMP_Text tiempoVuelta;

    private float tTotal;
    private float tVuelta;

    public bool inicioCarrera = false;

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
        if(vueltaActual > 0)
        {
            tVuelta = 0;
        }
        vueltaActual++;
        numeroVuelta.text = vueltaActual.ToString() + "/3";
    }

    private void ActualizarTemporizador(TMP_Text texto, float tiempo)
    {
        int minutes = Mathf.FloorToInt(tiempo / 60F);
        int seconds = Mathf.FloorToInt(tiempo % 60F);
        texto.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

}
