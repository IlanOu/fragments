using System;
using System.Collections.Generic;
using NPC.NPCMovement;
using UnityEngine;

public class ActionDirector : MonoBehaviour
{
    [Serializable]
    public class MovementStep
    {
        public NPCMovementType movementType;
        public MovementParameters parameters = new MovementParameters();
    }
    
    [SerializeField] private List<MovementStep> movementSequence = new List<MovementStep>();
    [SerializeField] private float detectionRadius = 2f; // Rayon de détection des NPCs
    
    private Dictionary<NpcMovement, float> processedNPCs = new Dictionary<NpcMovement, float>();
    
    private void Start()
    {
        // S'abonner à l'événement de rewind
        if (TimeRewindManager.Instance != null)
        {
            TimeRewindManager.Instance.OnRewindComplete += OnRewindComplete;
        }
        
        // Vérifier les NPCs déjà présents au démarrage
        CheckForNPCs();
    }
    
    private void OnDestroy()
    {
        // Se désabonner de l'événement
        if (TimeRewindManager.Instance != null)
        {
            TimeRewindManager.Instance.OnRewindComplete -= OnRewindComplete;
        }
    }
    
    private void OnRewindComplete(float rewindTime)
    {
        Debug.Log($"Zone {gameObject.name}: Rewind complete to time {rewindTime}");
        
        // Réinitialiser les NPCs qui ont été traités après le temps de rewind
        List<NpcMovement> npcsToRemove = new List<NpcMovement>();
        
        foreach (var kvp in processedNPCs)
        {
            NpcMovement npc = kvp.Key;
            float processTime = kvp.Value;
            
            if (processTime > rewindTime)
            {
                npcsToRemove.Add(npc);
            }
        }
        
        foreach (var npc in npcsToRemove)
        {
            processedNPCs.Remove(npc);
        }
        
        // Vérifier à nouveau les NPCs après le rewind
        CheckForNPCs();
    }
    
    private void Update()
    {
        // Vérifier périodiquement les NPCs
        CheckForNPCs();
    }
    
    private void CheckForNPCs()
    {
        // Obtenir le temps actuel
        float currentTime = TimeRewindManager.Instance != null ? 
            (TimeRewindManager.Instance.IsRewinding ? TimeRewindManager.Instance.CurrentPlaybackTime : TimeRewindManager.Instance.RecordingTime) : 
            Time.time;
        
        // Trouver tous les NPCs dans la scène
        NpcMovement[] allNPCs = FindObjectsOfType<NpcMovement>();
        
        foreach (NpcMovement npc in allNPCs)
        {
            if (npc == null) continue;
            
            float distance = Vector3.Distance(npc.transform.position, transform.position);
            
            // Si le NPC est dans la zone et n'a pas encore été traité (ou a été réinitialisé par un rewind)
            if (distance <= detectionRadius && !processedNPCs.ContainsKey(npc))
            {
                Debug.Log($"Zone {gameObject.name}: NPC {npc.name} entered zone at time {currentTime}");
                TriggerMovements(npc);
                processedNPCs[npc] = currentTime;
            }
            // Si le NPC est sorti de la zone
            else if (distance > detectionRadius && processedNPCs.ContainsKey(npc))
            {
                Debug.Log($"Zone {gameObject.name}: NPC {npc.name} left zone");
                processedNPCs.Remove(npc);
            }
        }
    }
    
    private void TriggerMovements(NpcMovement npc)
    {
        // Créer les commandes
        MovementCommand[] commands = new MovementCommand[movementSequence.Count];
        for (int i = 0; i < movementSequence.Count; i++)
        {
            MovementStep step = movementSequence[i];
            commands[i] = new MovementCommand(step.movementType, step.parameters);
        }
        
        // Exécuter la séquence
        npc.QueueMovementSequence(commands);
    }
    
    // Visualiser la zone de détection dans l'éditeur
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
