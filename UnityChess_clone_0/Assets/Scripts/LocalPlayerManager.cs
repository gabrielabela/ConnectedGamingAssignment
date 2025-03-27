using UnityChess;
using UnityEngine;

public class LocalPlayerManager : MonoBehaviour
{
    public static LocalPlayerManager Instance { get; private set; }

    public Side MySide { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // add this line
        }
    }

}
