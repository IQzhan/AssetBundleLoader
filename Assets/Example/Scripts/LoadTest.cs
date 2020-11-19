using E;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadTest : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        DontDestroyOnLoad(new GameObject("LoadTest").AddComponent<LoadTest>().gameObject);

        //Example
        //Load a scene
        AssetBundleLoader.LoadScene("Example/Res/Scenes/Scene01/Scene01", (Scene scene01) =>
        {
            if (scene01 != default)
            {
                //Load an asset whith extension
                AssetBundleLoader.LoadAsset("Example/Res/Prefabs/Sphere.prefab", (GameObject sphere) =>
                {
                    for(int i = 0; i < 20; i++)
                    {
                        GameObject obj = Instantiate(sphere);
                        RandomMove randomMove = obj.GetComponent<RandomMove>();
                        randomMove.during = Random.Range(0.03f, 0.18f);
                    }
                });
            }
        }, LoadSceneMode.Additive);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}
