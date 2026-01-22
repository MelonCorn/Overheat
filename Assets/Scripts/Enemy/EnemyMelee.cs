using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;


[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMelee : EnemyBase
{
    private NavMeshAgent _agent;

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
    }
    protected override void OnEnable()
    {
        base.OnEnable();

        if (_agent != null)
        {
            _agent.enabled = true;
            _agent.isStopped = false;
        }
    }


    // Çàµ¿
    protected override void Think()
    {

    }
}
