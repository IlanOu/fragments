using System;
using System.Collections.Generic;
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
    
    private void Start()
    {
        // // Vérifier les NPCs déjà présents
        // Collider[] colliders = Physics.OverlapBox(transform.position, GetComponent<Collider>().bounds.extents);
        // foreach (Collider col in colliders)
        // {
        //     Movement npc = col.GetComponent<Movement>();
        //     if (npc != null)
        //     {
        //         TriggerMovements(npc);
        //     }
        // }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("ON triugger");
        Movement npc = other.GetComponent<Movement>();
        if (npc != null)
        {
            TriggerMovements(npc);
        }
    }
    
    private void TriggerMovements(Movement npc)
    {
        Debug.Log("TriggerMovements");
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
}