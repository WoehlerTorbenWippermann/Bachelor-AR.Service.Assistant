using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.MyScripts.Tutorial
{
    /// <summary>
    /// Loads the tutorial scene. Attach this script to the tutorial button.
    /// </summary>
    public class TutorialLoader : MonoBehaviour
    {
        [Tooltip("Exact name of the tutorial scene (as entered in the Build Settings)")]
        [SerializeField] private string tutorialSceneName = "MyTutorial";

        public void StartTutorial()
        {
            SceneManager.LoadScene(tutorialSceneName);
        }
    }
}
