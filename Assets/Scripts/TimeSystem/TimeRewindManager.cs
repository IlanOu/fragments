using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using Minis;
using UnityEngine;

/// <summary>
/// Central manager for the time rewind system.
/// Handles recording and rewinding of all rewindable objects.
/// </summary>
public class TimeRewindManager : MonoBehaviour
{
    // Singleton instance
    public static TimeRewindManager Instance { get; private set; }
    
    [Tooltip("Maximum recording time in seconds (5 minutes by default)")]
    [SerializeField] private float maxRecordTime = 300f;
    
    [Tooltip("Time interval between state recordings (in seconds)")]
    [SerializeField] private float recordInterval = 0.1f;
    
    [Tooltip("Mouse scroll sensitivity for time navigation")]
    [SerializeField] private float scrollSensitivity = 0.5f;
    
    [Header("Jog Wheel Settings")]
    [SerializeField] private float jogSensitivity = 0.5f;
    [SerializeField] private float jogInertiaFactor = 0.9f; // Facteur de décélération (0-1)
    [SerializeField] private float jogMinimumVelocity = 0.01f;
    
    
    private float _jogVelocity = 0f;
    private bool _isJogging = false;
    
    [Tooltip("Use adaptive recording to reduce memory usage")]
    [SerializeField] private bool useAdaptiveRecording = true;
    
    [Tooltip("Maximum objects to process per frame (for performance)")]
    [SerializeField] private int maxObjectsPerFrame = 5;
    
    // Internal state
    private List<ITimeRewindable> _rewindableObjects = new List<ITimeRewindable>();
    private float _recordingTime = 0f;
    private float _currentPlaybackTime = 0f;
    private bool _isRewinding = false;
    private float _lastRecordTime = 0f;
    private int _currentObjectIndex = 0;
    
    // Properties for external access
    public float RecordingTime => _recordingTime;
    public float CurrentPlaybackTime => _currentPlaybackTime;
    public bool IsRewinding => _isRewinding;
    
    public event Action<float> OnRewindComplete;
    
    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        // Find and initialize all rewindable objects in the scene
        _rewindableObjects.AddRange(
            FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, 
            FindObjectsSortMode.None)
            .OfType<ITimeRewindable>()
        );

        foreach (var mono in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mono is ITimeRewindable rewindable)
            {
                _rewindableObjects.Add(rewindable);
            }
        }
        
        foreach (ITimeRewindable obj in _rewindableObjects)
        {
            if (obj != null)
                obj.InitializeStateRecording(recordInterval, useAdaptiveRecording);
        }
        
        // Enregistrer les bindings MIDI
        MidiBindingRegistry.Instance.Bind(ActionEnum.ScrubTimeline, OnMidiJog);
        MidiBindingRegistry.Instance.Bind(ActionEnum.ToggleRewind, OnMidiRewindToggle);
    }
    
    private void OnMidiRewindToggle(MidiInput input)
    {
        // Si c'est une note ON (Velocity > 0), activer le rewind
        // Si c'est une note OFF (Velocity = 0), désactiver le rewind
        if (input.Velocity > 0)
        {
            // Note ON - Activer le mode rewind s'il n'est pas déjà actif
            if (!_isRewinding)
            {
                Debug.Log("MIDI: Activation du mode rewind");
                ActivateRewindMode();
            }
        }
        else
        {
            // Note OFF - Désactiver le mode rewind s'il est actif
            if (_isRewinding)
            {
                Debug.Log("MIDI: Désactivation du mode rewind");
                DeactivateRewindMode();
            }
        }
    }
    
    private void OnMidiJog(MidiInput input)
    {
        // Si c'est un message avec une valeur (déplacement du jog)
        if (_isRewinding) // Seulement en mode rewind
        {
            // Calcul de la vitesse de déplacement
            float raw = -(input.Value - 64);
            _jogVelocity = raw * jogSensitivity;
            _isJogging = Mathf.Abs(_jogVelocity) > jogMinimumVelocity;
            
            // Application immédiate du déplacement
            if (_isJogging)
            {
                float deltaTime = _jogVelocity * Time.deltaTime;
                SetPlaybackTime(CurrentPlaybackTime + deltaTime);
            }
        }
    }
    
    private void Update()
    {
        // Toggle rewind mode with spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleRewindMode();
        }

        if (_isRewinding)
        {
            // Gestion de l'inertie du jog wheel
            if (Mathf.Abs(_jogVelocity) > jogMinimumVelocity)
            {
                // Appliquer l'inertie actuelle
                float deltaTime = _jogVelocity * Time.deltaTime;
                SetPlaybackTime(CurrentPlaybackTime + deltaTime);
                
                // Réduire progressivement la vitesse (décélération)
                _jogVelocity *= jogInertiaFactor;
            }
            else
            {
                // Arrêter complètement si on est sous le seuil minimum
                _jogVelocity = 0f;
                _isJogging = false;
            }
            
            // Time navigation using mouse scroll wheel
            float scrollInput = Input.mouseScrollDelta.y * scrollSensitivity;
            if (scrollInput != 0)
            {
                _currentPlaybackTime = Mathf.Clamp(_currentPlaybackTime - scrollInput, 0f, _recordingTime);
            }
            
            // Apply time to objects (distributed across frames)
            ProcessObjectsInRewindMode();
        }
        else // Mode enregistrement
        {
            // En mode normal, réinitialiser la vitesse du jog
            _jogVelocity = 0f;
            _isJogging = false;
            
            // Record states at regular intervals
            _recordingTime += Time.deltaTime;
            if (_recordingTime > maxRecordTime)
                _recordingTime = maxRecordTime;
                
            if (_recordingTime >= _lastRecordTime + recordInterval)
            {
                _lastRecordTime = _recordingTime;
                
                // Record object states (distributed across frames)
                ProcessObjectsInRecordMode();
            }
        }
    }
    
    // Méthode pour activer le mode rewind
    private void ActivateRewindMode()
    {
        _isRewinding = true;
        _jogVelocity = 0f; // Réinitialiser la vitesse du jog
        _isJogging = false;
        _currentPlaybackTime = _recordingTime;
        Debug.Log("Rewind mode activated. Current time: " + _currentPlaybackTime);
    }
    
    // Méthode pour désactiver le mode rewind
    private void DeactivateRewindMode()
    {
        _isRewinding = false;
        _jogVelocity = 0f; // Réinitialiser la vitesse du jog
        _isJogging = false;
    
        // IMPORTANT: Truncate history at resume point
        float oldRecordingTime = _recordingTime;
        _recordingTime = _currentPlaybackTime;
    
        // Inform all objects to truncate their history
        foreach (ITimeRewindable obj in _rewindableObjects)
        {
            if (obj != null)
                obj.TruncateHistoryAfter(_currentPlaybackTime);
        }
    
        // Reset recording time to avoid jumps
        _lastRecordTime = _recordingTime;
    
        Debug.Log("Rewind mode deactivated. Resuming recording from T=" + _recordingTime);
    
        // Notifier que le rewind est terminé
        CompleteRewind(_currentPlaybackTime);
    }

    
    /// <summary>
    /// Process a subset of objects each frame in rewind mode
    /// </summary>
    private void ProcessObjectsInRewindMode()
    {
        int objectsProcessed = 0;
        int startIndex = _currentObjectIndex;
        
        while (objectsProcessed < maxObjectsPerFrame && objectsProcessed < _rewindableObjects.Count)
        {
            int index = (startIndex + objectsProcessed) % _rewindableObjects.Count;
            if (_rewindableObjects[index] != null)
                _rewindableObjects[index].RewindToTime(_currentPlaybackTime);
            
            objectsProcessed++;
        }
        
        _currentObjectIndex = (startIndex + objectsProcessed) % _rewindableObjects.Count;
    }
    
    /// <summary>
    /// Process a subset of objects each frame in record mode
    /// </summary>
    private void ProcessObjectsInRecordMode()
    {
        int objectsProcessed = 0;
        int startIndex = _currentObjectIndex;
        
        while (objectsProcessed < maxObjectsPerFrame && objectsProcessed < _rewindableObjects.Count)
        {
            int index = (startIndex + objectsProcessed) % _rewindableObjects.Count;
            if (_rewindableObjects[index] != null)
                _rewindableObjects[index].RecordState(_recordingTime);
            
            objectsProcessed++;
        }
        
        _currentObjectIndex = (startIndex + objectsProcessed) % _rewindableObjects.Count;
    }
    
    /// <summary>
    /// Toggle between rewind and record modes
    /// </summary>
    public void ToggleRewindMode()
    {
        if (!_isRewinding)
        {
            ActivateRewindMode();
        }
        else
        {
            DeactivateRewindMode();
        }
    }
    
    /// <summary>
    /// Register a new rewindable object with the manager
    /// </summary>
    /// <param name="obj">Object to register</param>
    public void RegisterRewindableObject(ITimeRewindable obj)
    {
        if (!_rewindableObjects.Contains(obj))
        {
            _rewindableObjects.Add(obj);
            obj.InitializeStateRecording(recordInterval, useAdaptiveRecording);
        }
    }
    
    /// <summary>
    /// Unregister a rewindable object from the manager
    /// </summary>
    /// <param name="obj">Object to unregister</param>
    public void UnregisterRewindableObject(ITimeRewindable obj)
    {
        if (_rewindableObjects.Contains(obj))
            _rewindableObjects.Remove(obj);
    }
    
    /// <summary>
    /// Set the current playback time in rewind mode
    /// </summary>
    /// <param name="time">Target playback time</param>
    public void SetPlaybackTime(float time)
    {
        if (_isRewinding)
        {
            _currentPlaybackTime = Mathf.Clamp(time, 0f, _recordingTime);
        }
    }
    
    /// <summary>
    /// Jump backward in time by the specified amount
    /// </summary>
    /// <param name="seconds">Seconds to jump backward</param>
    public void JumpBackward(float seconds)
    {
        if (_isRewinding)
        {
            SetPlaybackTime(_currentPlaybackTime - seconds);
        }
    }
    
    /// <summary>
    /// Jump forward in time by the specified amount
    /// </summary>
    /// <param name="seconds">Seconds to jump forward</param>
    public void JumpForward(float seconds)
    {
        if (_isRewinding)
        {
            SetPlaybackTime(_currentPlaybackTime + seconds);
        }
    }
    
    /// <summary>
    /// Clear all recorded history for all objects
    /// </summary>
    public void ClearAllHistory()
    {
        foreach (ITimeRewindable obj in _rewindableObjects)
        {
            if (obj != null)
                obj.ClearStates();
        }
        
        _recordingTime = 0f;
        _currentPlaybackTime = 0f;
        _lastRecordTime = 0f;
    }
    
    private void CompleteRewind(float rewindTime)
    {
        // Autres opérations de fin de rewind...
        
        // Déclencher l'événement
        OnRewindComplete?.Invoke(rewindTime);
    }

}