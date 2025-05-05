/* ------------------ NPCMovementDance ------------------ */
using UnityEngine;
using NPC.NPCAnimations;


class MovementSwim : MovementStrategy
{
    private readonly float swimDuration;
    private float timer = 0f;
    private bool launched = false;

    public MovementSwim(GameObject NPC, float duration = 3f)
        : base(NPC)
    {
        swimDuration = duration;
    }

    public override void StartMovement()
    {
        if (launched) return;
        launched = true;
        timer = 0f;

        // Bool ON
        NPCAnimBus.Bool(NPC,
            NPCAnimationsType.Swim,
            true);
    }

    public override bool IsDone
    {
        get
        {
            if (!launched) return false;

            timer += Time.deltaTime;

            if (timer >= swimDuration)
            {
                launched = false;

                // Bool OFF  <--  C’était la ligne manquante
                NPCAnimBus.Bool(NPC,
                    NPCAnimationsType.Swim,
                    false);

                return true;
            }
            return false;
        }
    }
}