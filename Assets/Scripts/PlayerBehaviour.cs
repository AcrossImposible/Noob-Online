using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using StarterAssets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(108)]
public class PlayerBehaviour : MonoBehaviour
{
    [SerializeField] Transform blockHighlightPrefab;
    [SerializeField] LayerMask layerMask;
    [SerializeField] bool allowDigging;

    [SerializeField] int sizeMainInventory = 0;

    public bool IsOwner { get; set; } = true;

    //public static UnityEvent<Character> onMineSpawn = new UnityEvent<Character>();
    public static UnityEvent<MonoBehaviour> onMineSpawn = new UnityEvent<MonoBehaviour>();

    ThirdPersonController thirdPersonController;
    Transform blockHighlight;
    Character player;

    float deltaTime;

    private void Start()
    {
        blockHighlight = Instantiate(blockHighlightPrefab, Vector3.zero, Quaternion.identity);

        player = GetComponent<Character>();
        thirdPersonController = GetComponent<ThirdPersonController>();

        if (IsOwner)
        {
            onMineSpawn?.Invoke(this);
            EventsHolder.playerSpawnedMine?.Invoke(player);

            var sai = FindObjectOfType<StarterAssetsInputs>();
            var pi = FindObjectOfType<PlayerInput>();
            thirdPersonController.SetInput(sai, pi);

            var userDataPosition = UserData.Owner.position;
            //print($"{UserData.Owner.userName} ### {UserData.Owner.position}");
            if (userDataPosition == Vector3.zero)
            {
                transform.position += Vector3.one + Vector3.up * 180;
            }
            else
            {
                WorldGenerator.Inst.GetChunk(userDataPosition.ToGlobalRoundBlockPos());
                transform.position = userDataPosition + (Vector3.up * 5);
            }

            player.inventory.onTakeItem += Item_TakedUpdated;
            player.inventory.onUpdateItem += Item_TakedUpdated;

            InitSizeMainInventory();
            LoadInventory();
        }


        //FindPathSystem.Instance.onPathComplete += FindPath_Completed;
    }

    private void Item_TakedUpdated(Item item)
    {
        SaveInventory();
    }

    void InitSizeMainInventory()
    {
        player.inventory.mainSize = sizeMainInventory;
    }

    

    private void FindPath_Completed(FindPathSystem.PathDataResult data)
    {
        if (!data.found)
        {
            foreach (var item in data.explored)
            {
                WorldGenerator.Inst.SetBlockAndUpdateChunck(item, 66);
            }
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        deltaTime = Time.deltaTime;

        SavePlayerPosition();

        if (allowDigging)
        {
            BlockRaycast();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            var curType = CameraStack.Instance.CurrentType;
            if (curType == CameraStack.CameraType.First)
            {
                CameraStack.Instance.SwitchToThirdPerson();
            }
            else if (curType == CameraStack.CameraType.TopDown)
            {
                CameraStack.Instance.SwitchToFirstPerson();
            }
            else if (curType == CameraStack.CameraType.Third)
            {
                CameraStack.Instance.SwitchToTopDown();
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            var pos = transform.position + Vector3.right + Vector3.up + transform.forward;
            pos = pos.ToGlobalBlockPos();
            WorldGenerator.Inst.SetBlockAndUpdateChunck(pos, 8);
            targetPos = pos;
            print(pos);
            print(World.Instance.towerPos.position.ToGlobalBlockPos());

        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            //print(targetPos);
            FindPathSystem.Instance.Find(transform.position.ToGlobalBlockPos(), targetPos);
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            transform.position += Vector3.up * 80;
        }
    }

    Vector3 targetPos;

    void BlockRaycast()
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 8f, layerMask))
        {
            blockHighlight.position = Vector3.zero;

            Vector3 normalPos = hit.point - (hit.normal / 2);

            int x = Mathf.FloorToInt(normalPos.x);
            int y = Mathf.FloorToInt(normalPos.y);
            int z = Mathf.FloorToInt(normalPos.z);

            Vector3 blockPosition = new(x, y, z);

            blockHighlight.position = blockPosition;
            //blockHighlight.forward = Vector3.forward;

            if (Input.GetMouseButtonDown(0))
            {
                WorldGenerator.Inst.MineBlock(blockPosition + Vector3.right);
            }

            PlaceBlock(blockPosition + hit.normal);


            //if (Input.GetMouseButtonDown(1))
            //{
            //    // �����-�� ����� ���������� 1 �� ��� X, �� ������ ���, �� ������ ��� ��������
            //    ref var chunck = ref Service<World>.Get().GetChunk(blockPosition + Vector3.right);
            //    var pos = chunck.renderer.transform.position;

            //    // �����-�� ����� ���������� 1 �� ��� X, �� ������ ���, �� ������ ��� ��������
            //    int xBlock = x - Mathf.FloorToInt(pos.x) + 1;
            //    int yBlock = y - Mathf.FloorToInt(pos.y);
            //    int zBlock = z - Mathf.FloorToInt(pos.z);
            //    byte hitBlockID = chunck.blocks[xBlock, yBlock, zBlock];

            //    if (hitBlockID == 100 || hitBlockID == 101 || hitBlockID == 102)
            //    {
            //        GlobalEvents.interactBlockHited.Invoke(hitBlockID, new(x + 1, y, z));
            //    }
            //    else
            //    {
            //        int idx = 0;
            //        foreach (var entity in filter)
            //        {
            //            if (idx == InputHandler.Instance.quickSlotID - 1)
            //            {
            //                var poolItems = ecsWorld.GetPool<InventoryItem>();
            //                ref var item = ref poolItems.Get(entity);

            //                if (item.itemType == ItemType.Block)
            //                {
            //                    var e = godcraft.EcsWorld.NewEntity();

            //                    var pool = godcraft.EcsWorld.GetPool<ChunckHitEvent>();
            //                    pool.Add(e);
            //                    ref var component = ref pool.Get(e);
            //                    component.collider = hit.collider;
            //                    component.position = blockPosition + hit.normal;
            //                    component.blockId = item.blockID;

            //                    onChunkHit?.Invoke(new Entity { id = e }, component);
            //                    GlobalEvents.onBlockPlaced?.Invoke(item.blockID, blockPosition + hit.normal);

            //                    // HOT FIX ������� � ��������� �������
            //                    item.count--;
            //                    if (item.count == 0)
            //                    {
            //                        Destroy(item.view);
            //                        ecsWorld.DelEntity(entity);
            //                    }

            //                    StartCoroutine(Delay());

            //                    //-----------------------------------
            //                }
            //                else
            //                {
            //                    ref var used = ref ecsWorld.GetPool<ItemUsed>().Add(entity);
            //                    used.entity = entity;
            //                    used.id = item.blockID;

            //                    StartCoroutine(Delay());
            //                }

            //                IEnumerator Delay()
            //                {
            //                    yield return null;

            //                    GlobalEvents.itemUsing?.Invoke(entity);
            //                }

            //                break;
            //            }
            //            idx++;
            //        }


            //    }
            //}
        }
        else
        {
            blockHighlight.position = default;
        }
    }

    void PlaceBlock(Vector3 blockPosition)
    {
        if (Input.GetMouseButtonDown(1))
        {
            //print("kjdnsfjksdf");
            if (player.inventory.CurrentSelectedItem != null)
            {

                var item = player.inventory.CurrentSelectedItem;
                // �����-�� ����� ���������� 1 �� ��� X, �� ������ ���, �� ������ ��� ��������
                var generator = WorldGenerator.Inst;
                var chunck = generator.GetChunk(blockPosition + Vector3.right);
                var pos = chunck.renderer.transform.position;

                int xBlock = (int)(blockPosition.x - pos.x) + 1;
                int yBlock = (int)(blockPosition.y - pos.y);
                int zBlock = (int)(blockPosition.z - pos.z);
                // �����-�� ����� ���������� 1 �� ��� X, �� ������ ���, �� ������ ��� ��������
                byte hitBlockID = chunck.blocks[xBlock, yBlock, zBlock];

                chunck.blocks[xBlock, yBlock, zBlock] = item.id;

                var mesh = generator.UpdateMesh(chunck);//, (int)pos.x, (int)pos.y, (int)pos.z);
                chunck.meshFilter.mesh = mesh;
                chunck.collider.sharedMesh = mesh;

                for (int p = 0; p < 6; p++)
                {
                    var blockPos = new Vector3(xBlock, yBlock, zBlock);

                    Vector3 checkingBlockPos = blockPos + World.faceChecks[p];
                    var blockInOtherChunckPos = checkingBlockPos + pos;


                    if (!IsBlockChunk((int)checkingBlockPos.x, (int)checkingBlockPos.y, (int)checkingBlockPos.z))
                    {
                        var otherChunck = generator.GetChunk(checkingBlockPos + pos);

                        var otherMesh = generator.UpdateMesh(otherChunck);
                        otherChunck.meshFilter.mesh = otherMesh;
                        otherChunck.collider.sharedMesh = otherMesh;


                    }
                }

                WorldGenerator.Inst.PlaceBlock(blockPosition + Vector3.right, item.id);

                player.inventory.Remove(item);
            }
        }
    }

    bool IsBlockChunk(int x, int y, int z)
    {
        var size = WorldGenerator.size;
        if (x < 0 || x > size - 1 || y < 0 || y > size - 1 || z < 0 || z > size - 1)
            return false;
        else
            return true;
    }

    float savePositionTimer;
    void SavePlayerPosition()
    {
        savePositionTimer += deltaTime;

        if (savePositionTimer < 1)
            return;

        savePositionTimer = 0;

        UserData.Owner.position = transform.position;
        UserData.Owner.SaveData();
    }

    void LoadInventory()
    {
        if (PlayerPrefs.HasKey("inventory"))
        {
            var json = PlayerPrefs.GetString("inventory");
            var jsonInventory = JsonConvert.DeserializeObject<JsonInventory>(json);
            jsonInventory.SetInventoryData(player.inventory);
        }
    }

    void SaveInventory()
    {
        var jsonInventory = new JsonInventory(player.inventory);
        var json = JsonConvert.SerializeObject(jsonInventory);
        PlayerPrefs.SetString("inventory", json);
        PlayerPrefs.Save();
    }
}
