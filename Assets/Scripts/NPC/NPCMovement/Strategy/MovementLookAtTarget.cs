using UnityEngine;
using NPC.NPCAnimations;


class MovementLookAtTarget : MovementStrategy
{
    private readonly GameObject target;
    private readonly float holdDuration;

    private float timer;
    private bool launched;

    public MovementLookAtTarget(GameObject NPC,
                                  GameObject target,
                                  float duration)
        : base(NPC)
    {
        this.target = target;
        holdDuration = Mathf.Max(0f, duration);
    }

    public override void StartMovement()
    {
        if (launched || target == null) return;
        launched = true;
        timer = 0f;
    
        // On désactive la rotation automatique du NavMeshAgent
        MainAgent.updateRotation = false;
        
        // Désactiver Root Motion pendant le regard
        Animator animator = NPC.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    
        // On coupe l'anim Walk au cas où
        NPCAnimBus.Bool(NPC,
                        NPCAnimationsType.Walk, false);
    }

    public override bool IsDone
    {
        get
        {
            if (!launched || target == null) return false;

            Rotate();

            timer += Time.deltaTime;
            bool finished = timer >= holdDuration;

            if (finished)
            {
                MainAgent.updateRotation = true;   // on rend la main à l'agent
            
                // Réactiver Root Motion à la fin si nécessaire
                Animator animator = NPC.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.applyRootMotion = true;
                }
            }

            return finished;
        }
    }

    private void Rotate()
    {
        Transform me = NPC.transform;
        Vector3 dir = target.transform.position - me.position;
    
        if (dir.sqrMagnitude < 0.001f) return;

        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
        if (flatDir.sqrMagnitude < 0.001f) return;
    
        Quaternion targetRotation = Quaternion.LookRotation(flatDir);
    
        // Ajuster cette valeur pour une rotation plus fluide
        float rotationSpeed = 3.0f; 
    
        me.rotation = Quaternion.Slerp(
            me.rotation, 
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

}
