using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Enemy1 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public UnityEngine.AI.NavMeshAgent agent;
    public float speed = 1;

    public MeshRenderer current;

    public UnityEngine.AI.NavMeshPath navMeshPath;

    void Start()
    {
        navMeshPath = new UnityEngine.AI.NavMeshPath();
        Vector3 pathTarget = Camera.main.transform.position;
        pathTarget.y = 0;
        agent.CalculatePath(pathTarget, navMeshPath);
        if (navMeshPath.status != UnityEngine.AI.NavMeshPathStatus.PathComplete) {
            Destroy(gameObject);
        }
        current.enabled = false;
        agent.speed = 0;
        StartCoroutine(enableMesh());
    }

    // Update is called once per frame
    void Update()
    {   
        if (!agent.enabled)
            return;
        Vector3 targetPos = Camera.main.transform.position;
        targetPos.y = 0;
        agent.SetDestination(targetPos);
        // if (navMeshPath.status != NavMeshPathStatus.PathComplete)
            //destroyCurrent();
        agent.speed = speed;

    }

    public void disableEnemy(){
        agent.enabled = false;
        StartCoroutine(enabledEnemy());
    }

    IEnumerator enabledEnemy(){
        yield return new WaitForSeconds(3);
        agent.enabled = true;
    }
    IEnumerator enableMesh(){
        yield return new WaitForSeconds(0.5f);
        current.enabled = true;
        agent.speed = speed;
    }

    public void destroyCurrent(){
        agent.speed = 0;
        agent.enabled = false;
        GameCanvasScript.score++;
        GetComponentInChildren<Animator>().enabled = false;
        this.GetComponent<Collider>().enabled = false;
        turnToRed();
        StartCoroutine(turnOriginal());
    }


    private void turnToRed(){
        Renderer enemyRenderer = GetComponentInChildren<Renderer>();
        for (int m = 0; m < enemyRenderer.materials.Length; m++)
        {
            enemyRenderer.materials[m].color = Color.red;
        }
    }


    IEnumerator turnOriginal(){
        yield return new WaitForSeconds(0.5f);
        Renderer enemyRenderer = GetComponentInChildren<Renderer>();
        for (int m = 0; m < enemyRenderer.materials.Length; m++)
        {
            enemyRenderer.materials[m].color = Color.white;
        }
        Destroy(gameObject);
    }


}
