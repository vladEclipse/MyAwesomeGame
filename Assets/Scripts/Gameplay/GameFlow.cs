using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Общие действия над партией: перезапуск сцены и выход из игры.
/// </summary>
public static class GameFlow
{
    public static void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
