using UnityEngine;

namespace FrostWind.Utils
{

public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
{
    public static T Instance { get; private set; }
    
    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"Multiple {typeof(T).Name} in scene! existing: {Instance}, new: {gameObject}");
            return;
        }
        Instance         = (T) this;
    }
}

}