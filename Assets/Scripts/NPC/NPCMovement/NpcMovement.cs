using System.Collections.Generic;
using NPC.NPCMovement.Strategy;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace NPC.NPCMovement
{
    public class NpcMovement : MonoBehaviour
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
                    return new TalkStrategy(gameObject, parameters.audioClip);
                
                case NPCMovementType.Walk:
                    return new WalkStrategy(gameObject, parameters.speed, parameters.duration);
                
                case NPCMovementType.WalkToLocation:
                    if (parameters.targetObject != null)
                        return new WalkToLocationStrategy(gameObject, parameters.targetObject);
                    else
                    {
                        Debug.LogWarning("WalkToLocation nécessite un objet cible");
                        return null;
                    }
                
                case NPCMovementType.LookAtTarget:
                    if (parameters.targetObject != null)
                        return new LookAtTargetStrategy(gameObject, parameters.targetObject, parameters.duration);
                    else
                    {
                        Debug.LogWarning("LookAtTarget nécessite un objet cible");
                        return null;
                    }
                
                case NPCMovementType.Dance:
                    return new DanceStrategy(gameObject, parameters.duration);
                
                case NPCMovementType.Yell:
                    return new YellStrategy(gameObject, parameters.audioClip);
                
                case NPCMovementType.Swim:
                    return new SwimStrategy(gameObject, parameters.duration);
                
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
        
        
        
        // Ajoutez ces méthodes à votre classe Movement existante
        // pour le support du système de rewind

        // Méthode pour obtenir l'état actuel
        public MovementState GetCurrentState()
        {
            MovementState state = new MovementState();
            state.IsExecutingMovement = _isExecutingMovement;
            
            // Copier la file d'attente
            state.PendingMovements = new List<MovementCommand>(_pendingMovements);
            
            // Capturer la stratégie actuelle
            if (_currentStrategy != null)
            {
                state.CurrentMovementType = GetMovementTypeFromStrategy(_currentStrategy);
                state.CurrentParameters = GetParametersFromStrategy(_currentStrategy);
            }
            
            return state;
        }

        // Méthode pour restaurer l'état
        public void RestoreState(MovementState state)
        {
            if (state == null) return;
            
            // Arrêter tous les mouvements en cours
            StopAllMovements();
            
            // Restaurer l'état
            _isExecutingMovement = state.IsExecutingMovement;
            _pendingMovements = new Queue<MovementCommand>(state.PendingMovements);
            
            // Restaurer la stratégie actuelle si nécessaire
            if (state.IsExecutingMovement && state.CurrentMovementType != NPCMovementType.Talk)
            {
                _currentStrategy = CreateStrategy(state.CurrentMovementType, state.CurrentParameters);
                
                if (_currentStrategy != null)
                {
                    onMovementStart.Invoke(_currentStrategy);
                    _currentStrategy.StartMovement();
                    StartCoroutine(WaitForMovementCompletion());
                }
            }
        }

        private NPCMovementType GetMovementTypeFromStrategy(MovementStrategy strategy)
        {
            if (strategy is WalkStrategy) return NPCMovementType.Walk;
            if (strategy is WalkToLocationStrategy) return NPCMovementType.WalkToLocation;
            if (strategy is TalkStrategy) return NPCMovementType.Talk;
            if (strategy is LookAtTargetStrategy) return NPCMovementType.LookAtTarget;
            if (strategy is YellStrategy) return NPCMovementType.Yell;
            if (strategy is SwimStrategy) return NPCMovementType.Swim;
            if (strategy is DanceStrategy) return NPCMovementType.Dance;
            return NPCMovementType.Talk;
        }

        private MovementParameters GetParametersFromStrategy(MovementStrategy strategy)
        {
            MovementParameters parameters = new MovementParameters();
            
            // Récupérer les paramètres de base en fonction du type
            // Vous pouvez simplifier cette partie si vous n'avez pas besoin
            // de tous les paramètres pour le rewind
            
            return parameters;
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
    
    // Ajoutez cette classe quelque part dans votre code
    [System.Serializable]
    public class MovementState
    {
        public bool IsExecutingMovement;
        public List<MovementCommand> PendingMovements = new List<MovementCommand>();
        public NPCMovementType CurrentMovementType = NPCMovementType.Talk;
        public MovementParameters CurrentParameters = new MovementParameters();
    }
}