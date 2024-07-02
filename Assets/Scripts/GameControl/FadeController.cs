using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    // Mediante este script se llevan a cabo los fundidos a negro en las partidas: cuando comienzan y se preparan la clasificación y la carrera o cuando se hace mal el recorrido
    // Es un Singleton para que se pueda invocar desde cualquier parte del código
    public static FadeController instance;
    // Duración de los fundidos
    public float fadeDuration = 0.3f;
    // Referencia a la imagen en negro del canvas
    private Image fadeImage;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        fadeImage = GetComponent<Image>();
    }

    // Corrutinas para hacer el fundido a negro de la pantalla
    public IEnumerator FadeOut()
    {
        // Se calcula el nivel de alfa de la imagen teniendo en cuenta el tiempo que se lleva haciendo el fundido y el máximo establecido
        float elapsedTime = 0f;
        Color color = fadeImage.color;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }
    }

    public IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        Color color = fadeImage.color;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            // En este caso, el alfa se modifica a la inversa, restando 1, ya que el valor va de 0 a 1
            color.a = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }
    }
}
