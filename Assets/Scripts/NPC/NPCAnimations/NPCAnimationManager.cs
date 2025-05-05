using System;
using System.Linq;
using NPC.NPCAnimations;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class NPCAnimationConfig
{
    public NPCAnimationsType animationType;       // Walk, Dance, Talk…
    public string animatorParameterName;          // "Walk", "Dance", "IsTalking"
    public bool useTriggerInsteadOfBool;          // true = Trigger, false = Bool
}

public class NPCAnimationManager : MonoBehaviour
{
    [Header("Configuration des liens Enum ↔ Paramètres Animator")]
    // public NPCAnimationConfig[] animationConfigs;

    [SerializeField] private Animator animator;

    [FormerlySerializedAs("npcMovementManager")] [SerializeField] private Movement movement;
    
    
    /* ─────────── Unity ─────────── */
    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError($"{name} : Animator manquant !");
    }

    // private void Start() => ValidateConfig();

    private void OnEnable()
    {
        NPCAnimBus.OnBool    += HandleBoolEvent;
        NPCAnimBus.OnTrigger += HandleTriggerEvent;
    }

    private void OnDisable()
    {
        NPCAnimBus.OnBool    -= HandleBoolEvent;
        NPCAnimBus.OnTrigger -= HandleTriggerEvent;
    }

    private void Start()
    {
        // npcMovementManager
    }

    /* ─────────── API publique (appels directs) ─────────── */
    // public void PlayAnimation(NPCAnimationsType type, bool value = true)
    // {
    //     var cfg = FindConfig(type);
    //     if (cfg == null) return;
    //
    //     if (cfg.useTriggerInsteadOfBool)
    //         animator.SetTrigger(cfg.animatorParameterName);
    //     else
    //         animator.SetBool(cfg.animatorParameterName, value);
    // }
    //
    // public void StopAnimation(NPCAnimationsType type)
    // {
    //     var cfg = FindConfig(type);
    //     if (cfg != null && !cfg.useTriggerInsteadOfBool)
    //         animator.SetBool(cfg.animatorParameterName, false);
    // }

    /* ─────────── Handlers du bus ─────────── */
    private void HandleBoolEvent(GameObject sender, NPCAnimationsType type, bool value)
    {
        // if (sender != gameObject) return;          // ignore les events destinés aux autres NPC
        // PlayAnimation(type, value);
    }

    private void HandleTriggerEvent(GameObject sender, NPCAnimationsType type)
    {
        // if (sender != gameObject) return;
        // PlayAnimation(type);                       // valeur bool ignorée pour Trigger
    }

    /* ─────────── Helpers internes ─────────── */
    // private NPCAnimationConfig FindConfig(NPCAnimationsType type)
    // {
    //     var cfg = animationConfigs.FirstOrDefault(c => c.animationType == type);
    //     if (cfg == null)
    //         Debug.LogWarning($"{name} : Animation {type} non configurée !");
    //     return cfg;
    // }
    //
    // private void ValidateConfig()
    // {
    //     if (animator == null) return;
    //
    //     foreach (var cfg in animationConfigs)
    //     {
    //         bool exists = animator.parameters.Any(p => p.name == cfg.animatorParameterName);
    //         if (!exists)
    //             Debug.LogError($"{name} : paramètre Animator manquant ⇒ {cfg.animatorParameterName}");
    //     }
    // }
}
