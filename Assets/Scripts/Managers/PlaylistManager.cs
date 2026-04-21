using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class PlaylistManager : MonoBehaviour
{
    private AudioSource audioSource;
    [SerializeField] private List<AudioClip> playlist = new List<AudioClip>();
    [SerializeField] private int indiceActual = 0;

    [Header("Configuración")]
    [SerializeField] private bool aleatorio = true;
    [SerializeField] private bool isPausedByScene = false;

    private static PlaylistManager instancia;
    private void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // Si ya existe uno, destruye el nuevo
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;

        // 1. Cargar audios de la carpeta Resources/Musica
        Object[] cargados = Resources.LoadAll("Music", typeof(AudioClip));
        foreach (var item in cargados)
        {
            playlist.Add((AudioClip)item);
        }

        // 2. Aplicar Shuffle si está activado
        if (aleatorio)
        {
            MixPlaylist();
        }

        if (playlist.Count > 0)
        {
            PlaySong(0);
        }
    }

    private void Update()
    {
        // Verifica si la canción terminó para pasar a la siguiente
        if (!audioSource.isPlaying && playlist.Count > 0 && !isPausedByScene)
        {
            NextSong();
        }
    }

    private void MixPlaylist()
    {
        // Algoritmo de Fisher-Yates para barajar la lista
        for (int i = 0; i < playlist.Count; i++)
        {
            AudioClip temp = playlist[i];
            int randomIndex = Random.Range(i, playlist.Count);
            playlist[i] = playlist[randomIndex];
            playlist[randomIndex] = temp;
        }
    }

    private void PlaySong(int indice)
    {
        indiceActual = indice;
        audioSource.clip = playlist[indiceActual];
        audioSource.Play();
    }

    private void NextSong()
    {
        indiceActual = (indiceActual + 1) % playlist.Count;
        PlaySong(indiceActual);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnLoadingScene;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLoadingScene;
    }

    
    private void OnLoadingScene(Scene scene, LoadSceneMode modo)
    {
        if (scene.name == "Nivel1")
        {
            isPausedByScene = true;
            //Parar la música
            audioSource.Pause();

            // Opción B: Destruir el objeto para que no estorbe en el juego
            //Destroy(this.gameObject);
        }
    }
}