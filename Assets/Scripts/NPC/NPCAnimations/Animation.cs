using NPC.NPCMovement;
using NPC.NPCMovement.Strategy;
using UnityEngine;

namespace NPC.NPCAnimations
{
    public class Animation : MonoBehaviour
    {
        // Référence à l'enfant qui contient l'Animator
        [SerializeField] private Animator childAnimator;
    
        // Noms des animations dans l'Animator
        [SerializeField] private string idleAnimationName = "Idle";
        [SerializeField] private string walkAnimationName = "Walk";
        [SerializeField] private string talkAnimationName = "Talk";
        [SerializeField] private string yellAnimationName = "Yell";
        [SerializeField] private string swimAnimationName = "Swim";
        [SerializeField] private string danceAnimationName = "Dance";
        [SerializeField] private string lookAtAnimationName = "LookAt";
    
        // Durée de la transition entre les animations (en secondes)
        [SerializeField] private float transitionDuration = 0.25f;
    
        private Movement movement;
    
        private void Awake()
        {
            movement = GetComponent<Movement>();
        
            // Trouver l'Animator s'il n'est pas déjà assigné
            if (childAnimator == null)
            {
                childAnimator = GetComponentInChildren<Animator>();
            }
        
            // S'abonner aux événements du Movement
            if (movement != null)
            {
                movement.onMovementStart.AddListener(OnMovementStart);
                movement.onMovementEnd.AddListener(OnMovementEnd);
            }
        }
    
        private void OnMovementStart(MovementStrategy strategy)
        {
            if (childAnimator == null) return;
        
            // Faire une transition douce vers l'animation appropriée
            string animationName = GetAnimationNameForStrategy(strategy);
            if (!string.IsNullOrEmpty(animationName))
            {
                childAnimator.CrossFade(animationName, transitionDuration);
            }
        }
    
        private void OnMovementEnd(MovementStrategy strategy)
        {
            if (childAnimator == null) return;
        
            // Revenir à l'animation d'idle avec une transition douce
            childAnimator.CrossFade(idleAnimationName, transitionDuration);
        }
    
        private string GetAnimationNameForStrategy(MovementStrategy strategy)
        {
            if (strategy is MovementWalk || strategy is MovementWalkToLocation)
                return walkAnimationName;
            else if (strategy is MovementTalk)
                return talkAnimationName;
            else if (strategy is MovementYell)
                return yellAnimationName;
            else if (strategy is MovementSwim)
                return swimAnimationName;
            else if (strategy is MovementDance)
                return danceAnimationName;
            else if (strategy is MovementLookAtTarget)
                return lookAtAnimationName;
        
            return idleAnimationName;
        }
    
        // Méthode publique pour jouer une animation spécifique avec transition
        public void PlayAnimationWithTransition(string animationName, float customTransitionDuration = -1)
        {
            if (childAnimator == null) return;
        
            float duration = customTransitionDuration > 0 ? customTransitionDuration : transitionDuration;
            childAnimator.CrossFade(animationName, duration);
        }
    }
}
