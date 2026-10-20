using UnityEngine;
using UnityEngine.SceneManagement;

// [AI] 게임 메인(타이틀) 화면 버튼 동작.
// "게임 시작" 버튼 -> gameSceneName에 적힌 씬(기본값 "2nd")으로 이동
// "나가기" 버튼 -> 게임 종료 (에디터에서 Play 중이면 Play를 멈춘다)
public class MainMenuController : MonoBehaviour
{
    [Tooltip("게임 시작 버튼을 누르면 이동할 씬 이름")]
    public string gameSceneName = "2nd";

    public void OnClickStart()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
