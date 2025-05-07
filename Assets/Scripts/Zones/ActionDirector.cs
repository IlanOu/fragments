using System;
using System.Collections.Generic;
using System.Linq;
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
    
    // Dictionnaire pour suivre les NPCs qui sont entrés dans la zone
    private Dictionary<NpcMovement, float> processedNPCs = new Dictionary<NpcMovement, float>();
    
    // Ensemble pour suivre les actions déjà déclenchées à des moments spécifiques
    private HashSet<string> processedActions = new HashSet<string>();
    
    private void Start()
    {
        // S'abonner à l'événement de rewind
        if (TimeRewindManager.Instance != null)
        {
            TimeRewindManager.Instance.OnRewindComplete += OnRewindComplete;
        }
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
        
        // Réinitialiser les actions déclenchées après le temps de rewind
        List<string> actionsToRemove = new List<string>();
        
        foreach (var actionKey in processedActions)
        {
            // Le format de la clé est "instanceID_startTime"
            string[] parts = actionKey.Split('_');
            if (parts.Length >= 2 && float.TryParse(parts[1], out float actionTime))
            {
                if (actionTime > rewindTime)
                {
                    actionsToRemove.Add(actionKey);
                }
            }
        }
        
        foreach (var actionKey in actionsToRemove)
        {
            processedActions.Remove(actionKey);
        }
    }
    
    private void Update()
    {
        // Obtenir le temps actuel de la timeline
        float currentTime = TimeRewindManager.Instance != null ? 
            (TimeRewindManager.Instance.IsRewinding ? TimeRewindManager.Instance.CurrentPlaybackTime : TimeRewindManager.Instance.RecordingTime) : 
            Time.time;
        
        // Vérifier les NPCs dans la zone
        NpcMovement[] npcsInZone = GetNPCsInZone();
        
        foreach (NpcMovement npc in npcsInZone)
        {
            // Pour chaque mouvement dans la séquence
            foreach (var step in movementSequence)
            {
                // Si ce mouvement a un startTime défini et qu'on est à ce moment précis
                if (step.parameters.startTime > 0 && Mathf.Abs(currentTime - step.parameters.startTime) < 0.1f)
                {
                    // Vérifier si ce NPC a déjà exécuté cette action à ce temps
                    string actionKey = $"{npc.GetInstanceID()}_{step.parameters.startTime}";
                    
                    if (!processedActions.Contains(actionKey))
                    {
                        Debug.Log($"Zone {gameObject.name}: Triggering timed action {step.movementType} at time {step.parameters.startTime} for NPC {npc.name}");
                        
                        // Déclencher l'action
                        TriggerSingleMovement(npc, step);
                        
                        // Marquer comme traitée
                        processedActions.Add(actionKey);
                    }
                }
            }
            
            // Vérifier également si le NPC vient d'entrer dans la zone (comportement original)
            if (!processedNPCs.ContainsKey(npc))
            {
                Debug.Log($"Zone {gameObject.name}: NPC {npc.name} entered zone at time {currentTime}");
                
                // Déclencher uniquement les mouvements sans startTime
                TriggerNonTimedMovements(npc);
                
                processedNPCs[npc] = currentTime;
            }
            // Si le NPC est sorti de la zone
            else if (Vector3.Distance(npc.transform.position, transform.position) > detectionRadius)
            {
                Debug.Log($"Zone {gameObject.name}: NPC {npc.name} left zone");
                processedNPCs.Remove(npc);
            }
        }
    }
    
    private NpcMovement[] GetNPCsInZone()
    {
        // Trouver tous les NPCs dans la scène
        NpcMovement[] allNPCs = FindObjectsOfType<NpcMovement>();
        List<NpcMovement> npcsInZone = new List<NpcMovement>();
        
        foreach (NpcMovement npc in allNPCs)
        {
            if (npc == null) continue;
            
            float distance = Vector3.Distance(npc.transform.position, transform.position);
            
            // Si le NPC est dans la zone
            if (distance <= detectionRadius)
            {
                npcsInZone.Add(npc);
            }
        }
        
        return npcsInZone.ToArray();
    }
    
    // Méthode pour déclencher un seul mouvement
    private void TriggerSingleMovement(NpcMovement npc, MovementStep step)
    {
        // Créer une copie des paramètres pour ne pas modifier l'original
        MovementParameters parameters = new MovementParameters();
        parameters.startTime = step.parameters.startTime;
        parameters.targetObject = step.parameters.targetObject;
        parameters.audioClip = step.parameters.audioClip;
        parameters.duration = step.parameters.duration;
        parameters.speed = step.parameters.speed;
        
        // Exécuter l'action
        npc.QueueMovement(step.movementType, parameters);
    }
    
    // Méthode pour déclencher uniquement les mouvements sans startTime
    private void TriggerNonTimedMovements(NpcMovement npc)
    {
        List<MovementCommand> commands = new List<MovementCommand>();
        
        foreach (var step in movementSequence)
        {
            // Ne prendre que les mouvements sans startTime
            if (step.parameters.startTime <= 0)
            {
                commands.Add(new MovementCommand(step.movementType, step.parameters));
            }
        }
        
        if (commands.Count > 0)
        {
            // Exécuter la séquence
            npc.QueueMovementSequence(commands.ToArray());
        }
    }
    
    // Visualiser la zone de détection dans l'éditeur
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
