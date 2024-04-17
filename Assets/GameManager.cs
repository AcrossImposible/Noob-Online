using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] Worker workerPrefab;
    [SerializeField] public Transform workersParent;
    [SerializeField] bool autoSpawn;
    [SerializeField] int countWorkers = 50;
    public List<Player> players = new();

    [Header("���������� ��������� �������")]
    public float JumpTopThresold = 0.8f;
    public float JumpLowThresold = 0.5f;
    public float JumpForce = 30;
    public float JumpAvailabelDist = 1f;
    public float maxDiffrentHeight = 10;

    public static GameManager Inst;

    private void Awake()
    {
        Inst = this;

        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        FindPathSystem.Instance.onPathComplete += FindPathBetweenBlocks_Completed;
        Player.onSpawn.AddListener(PlayerAny_Spawned);
    }

    private IEnumerator Start()
    {
        if (autoSpawn)
        {
            for (int i = 0; i < countWorkers; i++)
            {
                yield return new WaitForSeconds(8);

                var worker = Instantiate(workerPrefab);
                worker.transform.SetParent(workersParent);
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            var worker = Instantiate(workerPrefab);
            worker.transform.SetParent(workersParent);
        }
    }

    private void PlayerAny_Spawned(Player player)
    {
        players.Add(player);
    }

    public static void SetLayerByChild(GameObject go, int layer)
    {
        if (go.transform.childCount == 0)
            return;

        foreach (Transform t in go.transform)
        {
            t.gameObject.layer = layer;
            SetLayerByChild(t.gameObject, layer);
        }
    }

    public static void CheckPathBetweenBlock(Vector3 start, Vector3 end)
    {
        FindPathSystem.Instance.Find(start, end);
    }

    private void FindPathBetweenBlocks_Completed(FindPathSystem.PathDataResult data)
    {
        if (!data.found)
        {
            World.Instance.notMineable.AddRange(data.explored);
        }
    }
}
