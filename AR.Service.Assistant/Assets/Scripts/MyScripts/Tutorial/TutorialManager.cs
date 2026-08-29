using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.MyScripts.Tutorial
{
    /// <summary>
    /// Controls the tutorial flow: shows dialogs one after another.
    /// After the last dialog the app returns to the DialogExample scene.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Tooltip("All tutorial dialog GameObjects in the desired order")]
        [SerializeField] private GameObject[] dialogs;

        [Tooltip("Exact name of the main scene (as entered in the Build Settings)")]
        [SerializeField] private string mainSceneName = "DialogExample";

        private int currentIndex = 0;

        private void Start()
        {
            // Hide all dialogs, show only the first one
            for (int i = 0; i < dialogs.Length; i++)
            {
                dialogs[i].SetActive(i == 0);
            }
        }

        /// <summary>
        /// Called by the "Continue" button of each dialog.
        /// </summary>
        public void Next()
        {
            // Hide the current dialog
            dialogs[currentIndex].SetActive(false);
            currentIndex++;

            if (currentIndex < dialogs.Length)
            {
                // Show the next dialog
                dialogs[currentIndex].SetActive(true);
            }
            else
            {
                // Tutorial finished → back to the main scene
                SceneManager.LoadScene(mainSceneName);
            }
        }
    }
}
