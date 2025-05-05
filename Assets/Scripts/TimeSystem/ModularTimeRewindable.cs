using System;
using System.Collections.Generic;
using System.Linq;
using NPC.NPCMovement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

/* ───────────────────────────────── Enum ───────────────────────────────── */
[Flags]
public enum RewindModules
{
    Transform    = 1 << 0,
    NavMesh      = 1 << 1,
    Animator     = 1 << 2,
    NPCMovement  = 1 << 3
}

/* ────────────────────────── Composant principal ───────────────────────── */
[RequireComponent(typeof(Transform))]
public class ModularTimeRewindable : MonoBehaviour, ITimeRewindable
{
    [Header("Overrides")]
    [SerializeField] private NavMeshAgent       navMeshOverride;
    [SerializeField] private Animator           animatorOverride;
    [FormerlySerializedAs("npcMovementOverride")] [SerializeField] private Movement movementOverride;

    /* ================== Sélection des modules à suivre ================== */
    [Header("Modules à enregistrer")]
    public RewindModules modules = RewindModules.Transform;

    /* =================== Paramétrage de l’enregistrement ================= */
    [Header("Paramètres généraux")]
    [SerializeField] private float recordInterval = 0.1f;
    [SerializeField] private bool  useAdaptiveRecording = true;

    [Header("Seuils (Transform)")]
    [SerializeField] private float significantMovementThreshold  = 0.05f;
    [SerializeField] private float significantRotationThreshold = 5f;

    /* ────────────────────────── Snapshots internes ────────────────────── */
    [Serializable] private class TransformSnapshot
    {
        public Vector3   position;
        public Quaternion rotation;
    }

    [Serializable] private class NavMeshSnapshot
    {
        public Vector3 velocity;
        public Vector3 destination;
        public float   stoppingDistance;
    }

    [Serializable] private class AnimatorStateSnapshot
    {
        public int   stateHash;
        public float normalizedTime;
        public int   layerIndex;
    }

    [Serializable] private class AnimatorSnapshot
    {
        public Dictionary<string, float> floatParams = new();
        public Dictionary<string, int>   intParams   = new();
        public Dictionary<string, bool>  boolParams  = new();
        public List<AnimatorStateSnapshot> states    = new();
    }

    [Serializable] private class NPCMovementSnapshot
    {
        public MovementState state;
    }

    private class TimeState
    {
        public float time;
        public TransformSnapshot  transform;
        public NavMeshSnapshot    nav;
        public AnimatorSnapshot   animator;
        public NPCMovementSnapshot npc;
    }

    /* ─────────────────────────── Variables priv. ───────────────────────── */
    private readonly List<TimeState> timeStates = new();
    private int maxStates;

    // caches
    private NavMeshAgent _agent;
    private Animator _animator;
    private Movement _mgr;

    // cache pour l'enregistrement adaptatif
    private Vector3    lastPos;
    private Quaternion lastRot;

    // flag interne pour identifier qu'on est en cours de rewind
    private bool _rewinding = false;

    /* ────────────────────────── Unity LifeCycle ───────────────────────── */
    private void Awake()
    {
        if (Has(RewindModules.NavMesh))
            _agent = navMeshOverride ??
                     GetComponent<NavMeshAgent>() ??
                     GetComponentInChildren<NavMeshAgent>();

        if (Has(RewindModules.Animator))
            _animator = animatorOverride ??
                        GetComponent<Animator>() ??
                        GetComponentInChildren<Animator>();

        if (Has(RewindModules.NPCMovement))
            _mgr = movementOverride ??
                      GetComponent<Movement>() ??
                      GetComponentInChildren<Movement>();
    }

    private void OnEnable()  => TimeRewindManager.Instance?.RegisterRewindableObject(this);
    private void OnDisable() => TimeRewindManager.Instance?.UnregisterRewindableObject(this);

    /* ─────────────────────── ITimeRewindable ─────────────────────── */
    public void InitializeStateRecording(float interval, bool adaptive)
    {
        recordInterval       = interval;
        useAdaptiveRecording = adaptive;
        maxStates            = Mathf.CeilToInt(300f / recordInterval);   // ≈ 5 min
        timeStates.Clear();

        timeStates.Add(CaptureState(0f));

        if (Has(RewindModules.Transform))
        {
            lastPos = transform.position;
            lastRot = transform.rotation;
        }
    }

    public void RecordState(float time)
    {
        if (!ShouldRecord()) return;

        timeStates.Add(CaptureState(time));

        while (timeStates.Count > maxStates)
            timeStates.RemoveAt(0);
    }

    public void TruncateHistoryAfter(float timePoint)
    {
        int idx = timeStates.FindLastIndex(s => s.time <= timePoint);
        if (idx < 0) return;

        if (idx < timeStates.Count - 1)
            timeStates.RemoveRange(idx + 1, timeStates.Count - idx - 1);

        timeStates[idx].time = timePoint;
    }

    public void ClearStates() => timeStates.Clear();

    /* ───────────────────────────── REWIND ───────────────────────────── */
    public void RewindToTime(float targetTime)
    {
        if (timeStates.Count < 2) return;
        _rewinding = true;

        /* ‑- recherche binaire pour trouver les deux états qui encadrent ‑- */
        int low = 0, high = timeStates.Count - 1, before = 0;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (timeStates[mid].time <= targetTime) { before = mid; low = mid + 1; }
            else high = mid - 1;
        }
        int after = Mathf.Min(before + 1, timeStates.Count - 1);

        /* ‑- Application / interpolation ‑- */
        if (before == after)
        {
            ApplyState(timeStates[before]);
        }
        else
        {
            TimeState A = timeStates[before];
            TimeState B = timeStates[after];

            float t = (B.time > A.time) ? Mathf.Clamp01((targetTime - A.time) / (B.time - A.time)) : 0f;

            /* Transform */
            if (Has(RewindModules.Transform))
            {
                transform.position = Vector3.Lerp(A.transform.position, B.transform.position, t);
                transform.rotation = Quaternion.Slerp(A.transform.rotation, B.transform.rotation, t);
            }

            /* NavMesh */
            if (Has(RewindModules.NavMesh) && _agent != null && _agent.isOnNavMesh)
            {
                // 1) Téléporte l’agent (supprime l’ancien path)
                _agent.Warp(transform.position);

                // 2) Fixe le nouveau path
                Vector3 dest = Vector3.Lerp(A.nav.destination, B.nav.destination, t);
                if (dest != transform.position)
                    _agent.SetDestination(dest);

                // Optionnel : remettre la vitesse enregistrée
                _agent.velocity         = Vector3.Lerp(A.nav.velocity, B.nav.velocity, t);
                _agent.stoppingDistance = Mathf.Lerp(A.nav.stoppingDistance, B.nav.stoppingDistance, t);
            }

            /* NPCMovement & Animator : on applique l’état "A" (pas besoin d’interpolation) */
            if (Has(RewindModules.NPCMovement) && A.npc != null) ApplyNPCSnapshot(A.npc);
            if (Has(RewindModules.Animator)    && A.animator != null) ApplyAnimatorSnapshot(A.animator);
        }

        _rewinding = false;
    }

    /* ───────────────────────────── Helpers ──────────────────────────── */
    private bool Has(RewindModules flag) => (modules & flag) != 0;

    private bool ShouldRecord()
    {
        if (!Has(RewindModules.Transform) || !useAdaptiveRecording || timeStates.Count == 0)
            return true;

        bool moved   = Vector3.Distance(transform.position, lastPos) > significantMovementThreshold;
        bool rotated = Quaternion.Angle(transform.rotation, lastRot) > significantRotationThreshold;

        if (moved || rotated)
        {
            lastPos = transform.position;
            lastRot = transform.rotation;
            return true;
        }
        return false;
    }

    /* ───────────────────── Captures & Applications ───────────────────── */

    private TimeState CaptureState(float time)
    {
        var s = new TimeState { time = time };

        /* Transform */
        if (Has(RewindModules.Transform))
        {
            s.transform = new TransformSnapshot
            {
                position = transform.position,
                rotation = transform.rotation
            };
        }

        /* NavMesh */
        if (Has(RewindModules.NavMesh) && _agent != null && _agent.isOnNavMesh)
        {
            s.nav = new NavMeshSnapshot
            {
                velocity        = _agent.velocity,
                destination     = _agent.hasPath ? _agent.destination : transform.position,
                stoppingDistance= _agent.stoppingDistance
            };
        }

        /* Animator */
        if (Has(RewindModules.Animator) && _animator != null)
        {
            s.animator = CaptureAnimatorSnapshot();
        }

        /* NPCMovement */
        if (Has(RewindModules.NPCMovement) && _mgr != null)
        {
            s.npc = CaptureNPCSnapshot();
        }

        return s;
    }

    /* ---- Animator helpers ---- */
    private AnimatorSnapshot CaptureAnimatorSnapshot()
    {
        var snap = new AnimatorSnapshot();

        foreach (var p in _animator.parameters)
        {
            switch (p.type)
            {
                case AnimatorControllerParameterType.Float: snap.floatParams[p.name] = _animator.GetFloat(p.name); break;
                case AnimatorControllerParameterType.Int:   snap.intParams[p.name]   = _animator.GetInteger(p.name); break;
                case AnimatorControllerParameterType.Bool:  snap.boolParams[p.name]  = _animator.GetBool(p.name); break;
            }
        }

        for (int i = 0; i < _animator.layerCount; i++)
        {
            var info = _animator.GetCurrentAnimatorStateInfo(i);
            snap.states.Add(new AnimatorStateSnapshot
            {
                layerIndex     = i,
                stateHash      = info.shortNameHash,
                normalizedTime = info.normalizedTime
            });
        }
        return snap;
    }

    private void ApplyAnimatorSnapshot(AnimatorSnapshot snap)
    {
        if (_animator == null || snap == null) return;

        // bool originalRootMotion = _animator.applyRootMotion;

        // RootMotion désactivé pendant le rewind
        // _animator.applyRootMotion = !_rewinding && originalRootMotion;

        foreach (var kv in snap.floatParams) _animator.SetFloat(kv.Key, kv.Value);
        foreach (var kv in snap.intParams)   _animator.SetInteger(kv.Key, kv.Value);
        foreach (var kv in snap.boolParams)  _animator.SetBool(kv.Key, kv.Value);

        foreach (var st in snap.states)
            _animator.Play(st.stateHash, st.layerIndex, st.normalizedTime);

        _animator.Update(0f); // force update sans avancer le temps

        // if (!_rewinding) _animator.applyRootMotion = originalRootMotion;
    }

    /* ---- NPCMovement helpers ---- */
    private NPCMovementSnapshot CaptureNPCSnapshot()
    {
        if (_mgr == null) return null;
    
        return new NPCMovementSnapshot
        {
            state = _mgr.GetCurrentState()
        };
    }

    private void ApplyNPCSnapshot(NPCMovementSnapshot snap)
    {
        if (_mgr == null || snap == null || snap.state == null) return;
    
        _mgr.RestoreState(snap.state);
    }


    private void ApplyState(TimeState s)
    {
        if (Has(RewindModules.Transform))
        {
            transform.position = s.transform.position;
            transform.rotation = s.transform.rotation;
        }

        if (Has(RewindModules.NavMesh) && _agent != null && _agent.isOnNavMesh)
        {
            // 1) Warp d’abord
            _agent.Warp(transform.position);

            // 2) Nouveau path
            if (s.nav.destination != transform.position)
                _agent.SetDestination(s.nav.destination);

            _agent.velocity         = s.nav.velocity;
            _agent.stoppingDistance = s.nav.stoppingDistance;
        }

        if (Has(RewindModules.NPCMovement) && s.npc != null)
            ApplyNPCSnapshot(s.npc);

        if (Has(RewindModules.Animator) && s.animator != null)
            ApplyAnimatorSnapshot(s.animator);
    }
}
