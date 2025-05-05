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
    
    private HashSet<NpcMovement> processedNPCs = new HashSet<NpcMovement>();
    
    private void Start()
    {
        // Vérifier les NPCs déjà présents au démarrage
        CheckForNPCs();
    }
    
    private void Update()
    {
        // Vérifier périodiquement les NPCs
        CheckForNPCs();
    }
    
    private void CheckForNPCs()
    {
        // Trouver tous les NPCs dans la scène
        NpcMovement[] allNPCs = FindObjectsOfType<NpcMovement>();
        
        Debug.Log($"Found {allNPCs.Length} NPCs in scene");
        
        foreach (NpcMovement npc in allNPCs)
        {
            float distance = Vector3.Distance(npc.transform.position, transform.position);
            
            Debug.Log($"NPC {npc.name} at distance {distance}, radius {detectionRadius}");
            
            // Si le NPC est dans la zone et n'a pas encore été traité
            if (distance <= detectionRadius && !processedNPCs.Contains(npc))
            {
                Debug.Log($"Triggering movements for NPC {npc.name}");
                TriggerMovements(npc);
                processedNPCs.Add(npc);
            }
            // Si le NPC est sorti de la zone
            else if (distance > detectionRadius && processedNPCs.Contains(npc))
            {
                Debug.Log($"NPC {npc.name} left the zone");
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
