using System.Collections.Generic;
using Nenn.InspectorEnhancements.Runtime.Attributes.Conditional;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class Movement : MonoBehaviour
{
    public NavMeshAgent MainAgent { get; private set; }
    private bool _isExecutingMovement = false;
    private MovementStrategy _currentStrategy;
    private Queue<MovementCommand> _pendingMovements = new Queue<MovementCommand>();
    
    public UnityEvent<MovementStrategy> onMovementStart;
    public UnityEvent<MovementStrategy> onMovementEnd;
    
    private void Awake()
    {
        MainAgent = GetComponent<NavMeshAgent>();
    }
    
    // Méthode pour ajouter un mouvement à la file d'attente
    public void QueueMovement(NPCMovementType movementType, MovementParameters parameters = null)
    {
        _pendingMovements.Enqueue(new MovementCommand(movementType, parameters ?? new MovementParameters()));
        
        // Si aucun mouvement n'est en cours, démarrer la séquence
        if (!_isExecutingMovement)
        {
            ProcessNextMovement();
        }
    }
    
    // Méthode pour exécuter immédiatement un mouvement (annule les mouvements en attente)
    public void DoMovement(NPCMovementType movementType, MovementParameters parameters = null)
    {
        // Vider la file d'attente
        _pendingMovements.Clear();
        
        // Arrêter le mouvement en cours
        StopCurrentMovement();
        
        // Ajouter et exécuter le nouveau mouvement
        QueueMovement(movementType, parameters);
    }
    
    // Méthode pour ajouter une séquence de mouvements
    public void QueueMovementSequence(MovementCommand[] sequence)
    {
        foreach (MovementCommand command in sequence)
        {
            _pendingMovements.Enqueue(command);
        }
        
        // Si aucun mouvement n'est en cours, démarrer la séquence
        if (!_isExecutingMovement)
        {
            ProcessNextMovement();
        }
    }
    
    // Traiter le prochain mouvement dans la file d'attente
    private void ProcessNextMovement()
    {
        if (_pendingMovements.Count == 0)
        {
            _isExecutingMovement = false;
            return;
        }
        
        _isExecutingMovement = true;
        MovementCommand command = _pendingMovements.Dequeue();
        
        MovementStrategy strategy = CreateStrategy(command.MovementType, command.Parameters);
        
        if (strategy != null)
        {
            _currentStrategy = strategy;
            
            onMovementStart.Invoke(strategy);
            strategy.StartMovement();
            
            StartCoroutine(WaitForMovementCompletion());
        }
        else
        {
            // Si la stratégie n'a pas pu être créée, passer au mouvement suivant
            ProcessNextMovement();
        }
    }
    
    private MovementStrategy CreateStrategy(NPCMovementType movementType, MovementParameters parameters)
    {
        switch (movementType)
        {
            case NPCMovementType.Talk:
                return new MovementTalk(gameObject, parameters.audioClip);
                
            case NPCMovementType.Walk:
                return new MovementWalk(gameObject, parameters.speed, parameters.duration);
                
            case NPCMovementType.WalkToLocation:
                if (parameters.targetObject != null)
                    return new MovementWalkToLocation(gameObject, parameters.targetObject);
                else
                {
                    Debug.LogWarning("WalkToLocation nécessite un objet cible");
                    return null;
                }
                
            case NPCMovementType.LookAtTarget:
                if (parameters.targetObject != null)
                    return new MovementLookAtTarget(gameObject, parameters.targetObject, parameters.duration);
                else
                {
                    Debug.LogWarning("LookAtTarget nécessite un objet cible");
                    return null;
                }
                
            case NPCMovementType.Dance:
                return new MovementDance(gameObject, parameters.duration);
                
            case NPCMovementType.Yell:
                return new MovementYell(gameObject, parameters.audioClip);
                
            case NPCMovementType.Swim:
                return new MovementSwim(gameObject, parameters.duration);
                
            default:
                Debug.LogWarning($"Type de mouvement non pris en charge: {movementType}");
                return null;
        }
    }
    
    private System.Collections.IEnumerator WaitForMovementCompletion()
    {
        while (!_currentStrategy.IsDone)
        {
            yield return null;
        }
        
        onMovementEnd.Invoke(_currentStrategy);
        
        // Passer au mouvement suivant
        ProcessNextMovement();
    }
    
    public void StopCurrentMovement()
    {
        if (_isExecutingMovement && _currentStrategy != null)
        {
            StopAllCoroutines();
            _isExecutingMovement = false;
            onMovementEnd.Invoke(_currentStrategy);
        }
    }
    
    // Arrêter tous les mouvements (en cours et en attente)
    public void StopAllMovements()
    {
        StopCurrentMovement();
        _pendingMovements.Clear();
    }
}

// Classe pour représenter une commande de mouvement
[System.Serializable]
public class MovementCommand
{
    public NPCMovementType MovementType;
    public MovementParameters Parameters;
    
    public MovementCommand(NPCMovementType movementType, MovementParameters parameters)
    {
        MovementType = movementType;
        Parameters = parameters;
    }
}

// Classe pour stocker tous les paramètres possibles
[System.Serializable]
public class MovementParameters
{
    public GameObject targetObject;
    public AudioClip audioClip;
    public float duration = 3f;
    public float speed = 1f;
}
