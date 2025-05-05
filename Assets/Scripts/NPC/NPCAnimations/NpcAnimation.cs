using NPC.NPCMovement;
using NPC.NPCMovement.Strategy;
using UnityEngine;

namespace NPC.NPCAnimations
{
    public class NpcAnimation : MonoBehaviour
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
    
        private NpcMovement _npcMovement;
        private string _currentAnimationName = ""; // Pour suivre l'animation en cours
        private int _currentAnimationHash = 0; // Hash de l'animation en cours
    
        private void Awake()
        {
            _npcMovement = GetComponent<NpcMovement>();
        
            // Trouver l'Animator s'il n'est pas déjà assigné
            if (childAnimator == null)
            {
                childAnimator = GetComponentInChildren<Animator>();
            }
        
            // S'abonner aux événements du Movement
            if (_npcMovement != null)
            {
                _npcMovement.onMovementStart.AddListener(OnMovementStart);
                _npcMovement.onMovementEnd.AddListener(OnMovementEnd);
            }
            
            // Initialiser avec l'animation idle
            _currentAnimationName = idleAnimationName;
            _currentAnimationHash = Animator.StringToHash(idleAnimationName);
        }
    
        private void OnMovementStart(MovementStrategy strategy)
        {
            if (childAnimator == null) return;
        
            // Obtenir le nom de l'animation pour cette stratégie
            string animationName = GetAnimationNameForStrategy(strategy);
            int animationHash = Animator.StringToHash(animationName);
            
            // Ne jouer l'animation que si elle est différente de l'animation en cours
            if (!string.IsNullOrEmpty(animationName) && animationHash != _currentAnimationHash)
            {
                childAnimator.CrossFade(animationName, transitionDuration);
                _currentAnimationName = animationName;
                _currentAnimationHash = animationHash;
            }
        }
    
        private void OnMovementEnd(MovementStrategy strategy)
        {
            if (childAnimator == null) return;
            
            bool hasMoreMovements = false;
            
            if (_npcMovement != null && typeof(NpcMovement).GetField("_pendingMovements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
            {
                var pendingMovementsField = typeof(NpcMovement).GetField("_pendingMovements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var pendingMovements = pendingMovementsField.GetValue(_npcMovement) as System.Collections.ICollection;
                hasMoreMovements = pendingMovements != null && pendingMovements.Count > 0;
            }
            
            
            if (!hasMoreMovements && _currentAnimationHash != Animator.StringToHash(idleAnimationName))
            {
                childAnimator.CrossFade(idleAnimationName, transitionDuration);
                _currentAnimationName = idleAnimationName;
                _currentAnimationHash = Animator.StringToHash(idleAnimationName);
            }
        }
    
        private string GetAnimationNameForStrategy(MovementStrategy strategy)
        {
            if (strategy is WalkStrategy || strategy is WalkToLocationStrategy)
                return walkAnimationName;
            else if (strategy is TalkStrategy)
                return talkAnimationName;
            else if (strategy is YellStrategy)
                return yellAnimationName;
            else if (strategy is SwimStrategy)
                return swimAnimationName;
            else if (strategy is DanceStrategy)
                return danceAnimationName;
            else if (strategy is LookAtTargetStrategy)
                return lookAtAnimationName;
        
            return idleAnimationName;
        }
    
        // Méthode publique pour jouer une animation spécifique avec transition
        public void PlayAnimationWithTransition(string animationName, float customTransitionDuration = -1)
        {
            if (childAnimator == null) return;
            
            int animationHash = Animator.StringToHash(animationName);
            
            // Ne jouer l'animation que si elle est différente de l'animation en cours
            if (animationHash != _currentAnimationHash)
            {
                float duration = customTransitionDuration > 0 ? customTransitionDuration : transitionDuration;
                childAnimator.CrossFade(animationName, duration);
                _currentAnimationName = animationName;
                _currentAnimationHash = animationHash;
            }
        }
        
        // Méthode pour obtenir l'animation en cours
        public string GetCurrentAnimationName()
        {
            return _currentAnimationName;
        }
        
        // Méthode pour vérifier si une animation spécifique est en cours
        public bool IsPlayingAnimation(string animationName)
        {
            return _currentAnimationHash == Animator.StringToHash(animationName);
        }
    }
}
