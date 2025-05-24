using Game;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Cinematics
{
    public class CinematicSequencer : MonoBehaviour
    {
        [SerializeField] private string nextSceneName;
        [SerializeField] private PlayableDirector cinematicDirector;

        private void Awake()
        {
            if (cinematicDirector == null)
                cinematicDirector = GetComponent<PlayableDirector>()
                                  ?? FindFirstObjectByType<PlayableDirector>();
        }

        private void Start()
        {
            if (cinematicDirector != null)
            {
                MidiBinding.Instance.Subscribe(MidiBind.IN_BUTTON_LONG_PRESS_1, (float value) => OnCinematicCompleted(cinematicDirector));
                MidiBinding.Instance.Subscribe(MidiBind.IN_BUTTON_LONG_PRESS_2, (float value) => OnCinematicCompleted(cinematicDirector));
                cinematicDirector.stopped += OnCinematicCompleted;
                if (cinematicDirector.state != PlayState.Playing)
                    cinematicDirector.Play();
            }
        }

        private void OnDestroy()
        {
            if (cinematicDirector != null)
                cinematicDirector.stopped -= OnCinematicCompleted;
        }

        private void OnCinematicCompleted(PlayableDirector _)
        {
            StartCoroutine(TransitionToNextSceneWithBlink());
        }

        private IEnumerator TransitionToNextSceneWithBlink()
        {

            SceneTransitionBlinker.Instance.TransitionToSceneWithVideo(nextSceneName);
            yield break;
        }
    }
}
