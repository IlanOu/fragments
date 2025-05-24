using System;
using System.Collections;
using System.Collections.Generic;
using Cinematics;
using UnityEngine;

namespace Steps
{
    public class StartStep: MonoBehaviour
    {
        [SerializeField] private string nextSceneName;
     
        [Header("If you want to listen to all inputs, leave the lists empty")]
        [SerializeField] private bool listenToAllKeys = true;
        [SerializeField] private bool listenToAllMouseButtons = true;
        [SerializeField] private bool listenToAllMidiActions = true;
        
        [Header("If you want to listen to specific inputs, specify them here")]
        // Listes optionnelles pour spécifier des entrées particulières si vous ne voulez pas tout écouter
        [SerializeField] private List<KeyCode> keysToMonitor = new List<KeyCode>();
        [SerializeField] private List<int> mouseButtonsToMonitor = new List<int>(); // 0=gauche, 1=droit, 2=milieu
        // [SerializeField] private List<ActionEnum> midiActionsToMonitor = new List<ActionEnum>();
        
        private bool hasStarted = false;
        
        void Start()
        {
            // Enregistrer les callbacks pour les actions MIDI
            if (listenToAllMidiActions)
            {
                // Écouter toutes les actions MIDI
                foreach (MidiBind bind in Enum.GetValues(typeof(MidiBind)))
                {
                    MidiBinding.Instance.Subscribe(bind, (float value) => TriggerOnStart());
                }
            }
        }

        void Update()
        {
            // Ne vérifier les entrées que si OnStart n'a pas encore été appelé
            if (hasStarted)
                return;
                
            // Vérifier les touches du clavier
            if (listenToAllKeys)
            {
                if (Input.anyKeyDown)
                {
                    TriggerOnStart();
                    return;
                }
            }
            else
            {
                foreach (KeyCode key in keysToMonitor)
                {
                    if (Input.GetKeyDown(key))
                    {
                        TriggerOnStart();
                        return;
                    }
                }
            }
            
            // Vérifier les boutons de souris
            if (listenToAllMouseButtons)
            {
                for (int i = 0; i < 3; i++) // 0=gauche, 1=droit, 2=milieu
                {
                    if (Input.GetMouseButtonDown(i))
                    {
                        TriggerOnStart();
                        return;
                    }
                }
            }
            else
            {
                foreach (int button in mouseButtonsToMonitor)
                {
                    if (Input.GetMouseButtonDown(button))
                    {
                        TriggerOnStart();
                        return;
                    }
                }
            }
        }
        
        private void TriggerOnStart()
        {
            if (!hasStarted)
            {
                hasStarted = true;
                OnStart();
            }
        }

        void OnStart()
        {
            StartCoroutine(DelayedTransition());
        }

        private IEnumerator DelayedTransition()
        {
            yield return new WaitUntil(() => SceneTransitionBlinker.Instance != null);

            SceneTransitionBlinker.Instance.TransitionToSceneWithVideo(nextSceneName);
        }
    }
}
