using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCMovementManager : MonoBehaviour
{
    [SerializeField] private List<NPCMovement> npcMovements;
    
    public NavMeshAgent MainAgent { get; private set; }
    public int CurrentIndex  { get; private set; } = 0;
    public IReadOnlyList<NPCMovement> Movements => npcMovements;

    private NPCMovement CurrentMovement =>
        npcMovements.Count == 0 || CurrentIndex >= npcMovements.Count
            ? null
            : npcMovements[CurrentIndex];

    /* ─────────────────── Unity ─────────────────── */
    private void Awake()
    {
        MainAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        foreach (var m in npcMovements)
            m.Init(this);
    }

    private void Update()
    {
        CurrentMovement?.Handle();
    }

    public void NextMovement()
    {
        CurrentIndex++;

        // planifie l'heure de départ du prochain mouvement, si besoin
        if (CurrentIndex < npcMovements.Count)
        {
            var next = npcMovements[CurrentIndex];
            if (next.TimeToStart < 0f)   // -1 => démarrer juste après le précédent
            {
                float t  = TimeRewindManager.Instance.RecordingTime;
                next.TimeToStart = t + next.delayAfterPrevious;
            }
        }
    }

    
    public void ForceState(int index, float[] startTimes, bool[] launched)
    {
        CurrentIndex = Mathf.Clamp(index, 0, npcMovements.Count - 1);

        for (int i = 0; i < npcMovements.Count; i++)
        {
            if (i < startTimes.Length)  npcMovements[i].TimeToStart = startTimes[i];
            if (i < launched.Length)    npcMovements[i].launched = launched[i];
        }
    }
}