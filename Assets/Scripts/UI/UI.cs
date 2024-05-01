using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;
using System;
using StarterAssets;
using TMPro;

public class UI : MonoBehaviour
{
    [SerializeField] bool testMobileInput;
    [SerializeField] Button btnHost;
    [SerializeField] Button btnClient;
    [SerializeField] InventotyView inventotyView;
    [SerializeField] QuickInventoryView quickInventoryView;
    [SerializeField] Button btnSwitchCamera;
    [SerializeField] UserView userView;
    [SerializeField] GameObject mobileController;
    [SerializeField] FixedTouchField touchField;
    [SerializeField] MobileInput mobileInput;
    [SerializeField] RectTransform touch1;
    [SerializeField] RectTransform touch2;
    [SerializeField] TMP_Text txtEbala;
    

    [Header("Output")]
    public StarterAssetsInputs starterAssetsInputs;

    [SerializeField] Button btnReset;

    public static UnityEvent onInventoryOpen = new UnityEvent();
    public static UnityEvent onInventoryClose = new UnityEvent();

    Character mine;
    Transform player;

    bool needResetPlayerPosition;

    private void Awake()
    {
        btnClient.onClick.AddListener(BtnClient_Clicked);
        btnHost.onClick.AddListener(BtnHost_Clicked);

        btnSwitchCamera.onClick.AddListener(BtnSwitchCamera_Clicked);

        btnReset.onClick.AddListener(BtnReset_Clicked);

        PlayerBehaviour.onMineSpawn.AddListener(PlayerMine_Spawned);
    }

    private void BtnReset_Clicked()
    {
        needResetPlayerPosition = true;
    }

    private void BtnSwitchCamera_Clicked()
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

    private void Start()
    {
        userView.Init();

        quickInventoryView.gameObject.SetActive(false);
        mobileController.SetActive(false);
        mobileInput.gameObject.SetActive(false);


#if UNITY_SERVER
        NetworkManager.Singleton.StartServer();
#endif

    }

    Vector2 lookDirection;
    Vector2 currentVelocity;
    public float smoothTime = 1f;
    public float sensitivity = 3f;
    private void Update()
    {
        if (Application.isMobilePlatform || testMobileInput)
        {
            lookDirection = Vector2.SmoothDamp(lookDirection, touchField.TouchDist * sensitivity, ref currentVelocity, Time.deltaTime * smoothTime);
            VirtualLookInput(lookDirection);
        }

        //TouchUpdate();
    }

    void TouchUpdate()
    {
        touch1.position = Vector3.down * 100;
        touch2.position = Vector3.down * 100;
        var mospos = Input.mousePosition;

        var offsetX = Screen.width / 2;
        var offsetY = Screen.height / 2;
        //touch1.position = new Vector3(mospos.x - offsetX, mospos.y - offsetY, 300);

        if (Input.touches.Length > 0)
        {
            touch1.position = Input.touches[0].position - new Vector2(offsetX, offsetY);
            touch1.position += (Vector3.forward * 300);

            if (Input.touches.Length > 1)
            {
                touch2.position = Input.touches[1].position - new Vector2(offsetX, offsetY);
                touch2.position += (Vector3.forward * 300);
            }
        }

        txtEbala.text = $"{touch1.position}";
    }

    private void BtnHost_Clicked()
    {
        NetworkManager.Singleton.StartHost();
    }

    private void BtnClient_Clicked()
    {
        NetworkManager.Singleton.StartClient();

    }

    private void PlayerMine_Spawned(MonoBehaviour player)
    {
        mobileInput.gameObject.SetActive(true);
        mobileInput.Init(player as PlayerBehaviour);

        mine = player.GetComponent<Character>();
        quickInventoryView.gameObject.SetActive(true);
        InitInventoryView(mine);

        if (Application.isMobilePlatform || testMobileInput)
        {
            mobileController.SetActive(true);
            touchField.gameObject.SetActive(true);
        }
        else
        {
            touchField.gameObject.SetActive(false);
        }

        this.player = player.transform;
    }

    private void InitInventoryView(Character player)
    {
        if (inventotyView)
        {
            inventotyView.Init(player.inventory);
        }
        quickInventoryView.Init(player.inventory);

        onInventoryOpen.AddListener(player.inventory.Open);
        onInventoryClose.AddListener(player.inventory.Close);
    }

   

    private void LateUpdate()
    {
        if (needResetPlayerPosition)
        {
            var pos = player.position;
            pos.y = 180;
            player.position = pos;
            needResetPlayerPosition = false;
        }
    }

    public static void ClearParent(Transform parent)
    {
        foreach (Transform item in parent)
        {
            Destroy(item.gameObject);
        }
    }

    public void VirtualLookInput(Vector2 virtualLookDirection)
    {
        starterAssetsInputs.LookInput(virtualLookDirection);
    }
}
