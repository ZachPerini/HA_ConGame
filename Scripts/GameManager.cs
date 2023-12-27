using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Networking.Transport;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;
using Random = UnityEngine.Random;
using Firebase.Database;

public enum CardAnimation
{
    Hidden,
    Revealing,
    Revealed
}

public struct Card
{
    public string CardName;
    public int CardValue;
    public GameObject CardGameObject;
    public CardAnimation AnimationState;

    public Card(string cardName, GameObject cardGameObject,CardAnimation animationState = CardAnimation.Hidden)
    {
        CardName = cardName;
        CardGameObject = cardGameObject;
        AnimationState = animationState;
        CardValue = 0;
    }
}

public struct PlayerData
{
    public int PlayerId;
    public Transform PlayerGameObject;
    public TMP_Text PlayerScoreUI;
    public Card Card;

    public int Score;
    public PlayerData(int playerId, Transform playerGameObject,TMP_Text playerScoreUI, Card card)
    {
        PlayerId = playerId;
        PlayerGameObject = playerGameObject;
        PlayerScoreUI = playerScoreUI;
        Card = card;
        Score = 0;
    }
}

public class GameManager : NetworkBehaviour
{
    [SerializeField] private PlayingCardsSO _playingCardsSO;
    [SerializeField] private int maximumScore = 3;
    [SerializeField] private float animationDuration = 2f;
    [SerializeField] private float roundTransitionDuration = 2f;
    [SerializeField] private GameObject playerPrefabA; //add prefab in inspector
    [SerializeField] private GameObject playerPrefabB; //add prefab in inspector
    private Dictionary<string, GameObject> _playingCards;
    private List<Texture2D> _backgrounds;
    public int Balance;
    public GameObject bk_1;
    public GameObject bk_2;
    public GameObject bk_3;
    public GameObject popupWindow;
    public GameObject SpriteAnchor;
    GameObject p2Score;
    GameObject newPlayer;
    private PlayerData _p1;
    private PlayerData _p2;
    private TextMeshProUGUI timeText;
    private NetworkVariable<float> time = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private Coroutine timerCoroutine;

    public string winner;
    public string loser;

    private Button _playBtnP1;
    private Button _playBtnP2;

    private static DatabaseReference _rtdReference;


    [SerializeField] private string _gameId;

    GameObject player_1;
    GameObject player_2;

    [SerializeField]
    private int requiredPlayers = 2;

    public int PlayerCount;

    private delegate void CardAnimationFinishedDelegate(PlayerData player);
    private event CardAnimationFinishedDelegate OnCardAnimationFinished;
    private delegate void ScoreChangedDelegate(PlayerData player);
    private event ScoreChangedDelegate OnScoreChanged;

    private static GameManager _instance;
    private NetworkObject netObj;
    private bool spawnClient;

    public static GameManager Singleton
    {
        get  => _instance;
        private set
        {
            if (_instance != null && _instance != value)
            {
                Debug.Log("Another Instance has beed detected");
                Destroy(value.gameObject);
            }
            _instance = value;
            DontDestroyOnLoad(value);
        }
    }
    private void Awake()
    {
        Singleton = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnCardAnimationFinished += CardAnimationFinished;
        OnScoreChanged += ScoreChanged;
        _playingCards = _playingCardsSO.PlayingCardsDict;
        _backgrounds = _playingCardsSO.Backgrounds;
        //DONT FORGET TO PRESS RESET DATA BUTTON WHEN ENTERING PLAYMODE
        _rtdReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    public static string GenerateGameId()
    {
        return "Game: " + Guid.NewGuid().ToString();
    }

    [ServerRpc(RequireOwnership = false)] //server owns this object but client can request a spawn
    public void SpawnPlayerServerRpc(ulong clientId, int prefabId)
    {
        GameObject newPlayer;
        if (prefabId == 0)
            newPlayer = (GameObject)Instantiate(playerPrefabA);
        else
            newPlayer = (GameObject)Instantiate(playerPrefabB);
        netObj = newPlayer.GetComponent<NetworkObject>();
        newPlayer.SetActive(true);
        netObj.SpawnAsPlayerObject(clientId, true);
        netObj.DestroyWithScene = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log("Spawn Host");
            SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId, 0);
        }
        else if (!IsServer && spawnClient == false)
        {
            SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId, 1);
            spawnClient = true;
        }  
    }

    private void OnClientConnected(ulong clientId)
    {
        popupWindow.SetActive(false);
        Debug.Log("All players joined");
        timerCoroutine = StartCoroutine(StartTimer());
        doServerRpc();
    }

    public void OnHostBTNClicked()
    {
        NetworkManager.Singleton.StartHost();
        if (NetworkManager.IsServer)
        {
            
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
    }

    public void OnJoinBTNClicked()
    {
        NetworkManager.Singleton.StartClient();
        
    }

    IEnumerator StartTimer()
    {
        while (true)
        {
            time.Value += Time.deltaTime;
            //timeText.text = "Time: " + time.Value.ToString("0.00");
            yield return null;
        }
    }

    private void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "WelcomeScene":
                GameObject.Find("StoreBtn").GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadSceneAsync("StoreScene"));
                GameObject.Find("HostBtn").GetComponent<Button>().onClick.AddListener(() => OnHostBTNClicked());
                GameObject.Find("JoinBtn").GetComponent<Button>().onClick.AddListener(() => OnJoinBTNClicked());
                GameObject.Find("ResetData").GetComponent<Button>().onClick.AddListener(() => ResetData());
                //NetworkManager.Singleton.Shutdown();
                break;
            case "GameScene":
                //timeText = GameObject.Find("Timer").GetComponent<TextMeshProUGUI>();
                if (PlayerPrefs.GetInt("ActiveBK1") == 1)
                {
                    Instantiate(bk_1);
                }
                if (PlayerPrefs.GetInt("ActiveBK2") == 1)
                {
                    Instantiate(bk_2);
                }
                if (PlayerPrefs.GetInt("ActiveBK3") == 1)
                {
                    Instantiate(bk_3);
                }
                if (IsServer)
                {
                    popupWindow = GameObject.FindGameObjectWithTag("PopUp");
                    popupWindow.SetActive(true);
                    NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                }
                if (!IsServer)
                {
                    popupWindow = GameObject.FindGameObjectWithTag("PopUp");
                    popupWindow.SetActive(false);
                    //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                }
                break;
            case "ScoreScene":
                GameObject.Find("BackBtn").GetComponent<Button>().onClick.AddListener(()=>SceneManager.LoadSceneAsync("WelcomeScene"));
                TMP_Text result = GameObject.Find("Winner").GetComponent<TMP_Text>();
                
                if (timerCoroutine != null)
                {
                    StopCoroutine(timerCoroutine);
                    timerCoroutine = null;
                }
                Debug.Log(time.Value.ToString());
                if (IsServer)
                {
                    player_1.GetComponent<NetworkObject>().Despawn();
                    player_2.GetComponent<NetworkObject>().Despawn();
                    //save to realtime
                    if (_p1.Score > _p2.Score)
                    {
                        result.text = "Player 1 Wins!";
                        _gameId = GenerateGameId();
                        winner = "Player 1";
                        loser = "Player 2";
                        GameData data = new GameData(_gameId, winner, loser, time.Value.ToString());
                        string json = JsonUtility.ToJson(data);
                        _rtdReference.Child("Games").Child(_gameId).Child("Timestamp: " + data.Timestamp.ToString()).Child("Winner: " + data.Winner).Child("Loser: " + data.Loser).SetRawJsonValueAsync(json);
                        Debug.Log(winner);
                        //NetworkManager.Singleton.Shutdown();
                    }
                    else
                    {
                        result.text = "Player 2 Wins!";
                        _gameId = GenerateGameId();
                        winner = "Player 2";
                        loser = "Player 1";
                        GameData data = new GameData(_gameId, winner, loser, time.Value.ToString());
                        string json = JsonUtility.ToJson(data);
                        _rtdReference.Child("Games").Child(_gameId).Child("Timestamp: " + data.Timestamp.ToString()).Child("Winner: " + data.Winner).Child("Loser: " + data.Loser).SetRawJsonValueAsync(json);
                        Debug.Log(winner);
                        //NetworkManager.Singleton.Shutdown();
                    }
                }
                if (!IsServer)
                {
                    //player_1.GetComponent<NetworkObject>().Despawn();
                    //player_2.GetComponent<NetworkObject>().Despawn();
                }
                //NetworkManager.Singleton.Shutdown();
                break;
            case "StoreScene":
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void doServerRpc(ServerRpcParams rpcParams = default)
    {
        doClientRpc();
    }

    [ClientRpc]
    public void doClientRpc()
    {
        Debug.Log("I called this from doClientRPC");
        player_1 = GameObject.FindGameObjectWithTag("Player1");
        _playBtnP1 = GameObject.Find("PlayBtnP1").GetComponent<Button>();
        _p1 = new PlayerData(1, player_1.transform, GameObject.FindGameObjectWithTag("p1Score").GetComponent<TMP_Text>(), new Card());
        player_2 = GameObject.FindGameObjectWithTag("Player2");
        _playBtnP2 = GameObject.Find("PlayBtnP2").GetComponent<Button>();
        _p2 = new PlayerData(2, player_2.transform, GameObject.FindGameObjectWithTag("p2Score").GetComponent<TMP_Text>(), new Card());
        if (IsServer)
        {
            //_playBtnP2.enabled = false;
            _playBtnP1.onClick.AddListener(() =>
            {
                player1PlayedClientRpc();
            });
            //_playBtnP1.enabled = false;
            _playBtnP2.onClick.AddListener(() =>
            {
                Debug.Log("clicked p2 button");
                player2PlayedClientRpc();
            });
        }
        if (!IsServer)
        {
            //_playBtnP1.enabled = false;
            //_playBtnP2.onClick.AddListener(() =>
            //{
            //    Debug.Log("clicked p2 button");
            //    player2PlayedClientRpc();
            //});
        }
        SpawnCardsClientRpc();
    }

    [ClientRpc]
    public void player1PlayedClientRpc()
    {
        _playBtnP1.enabled = false;
        StartCoroutine(RotateCard(_p1));
        Debug.Log("Player 1 played");
    }

    [ClientRpc]
    public void player2PlayedClientRpc()
    {
        _playBtnP2.enabled = false;
        StartCoroutine(RotateCard(_p2));
        Debug.Log("Player 2 played");
    }
    

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        OnCardAnimationFinished -= CardAnimationFinished;
        OnScoreChanged -= ScoreChanged;
    }

    

    public void ResetData() 
    {
        Debug.Log("ResetData");
        PlayerPrefs.SetInt("myCurrency", 1000);
        PlayerPrefs.SetInt("PurchasedBK1", 0);
        PlayerPrefs.SetInt("PurchasedBK2", 0);
        PlayerPrefs.SetInt("PurchasedBK3", 0);
        PlayerPrefs.SetInt("ActiveBK1", 0);
        PlayerPrefs.SetInt("ActiveBK2", 0);
        PlayerPrefs.SetInt("ActiveBK3", 0);
    }

    [ClientRpc]
    public void SpawnCardsClientRpc()
    {
        Debug.Log("Spawn Cards");
        DestroyCard(_p1);
        //Pick random card
        Card randCard = PickRandomCard();
        
        GameObject card = Instantiate(randCard.CardGameObject,_p1.PlayerGameObject.position,Quaternion.Euler(-90, 0, 180),_p1.PlayerGameObject);
        card.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        _p1.Card = new Card(randCard.CardName,card);
        int.TryParse(randCard.CardName.Substring(randCard.CardName.Length - 2), out _p1.Card.CardValue);
        
        
        //Clear and Spawn Player 2's card
        DestroyCard(_p2);
        randCard = PickRandomCard();
        int p2CardValTemp = 0;
        int.TryParse(randCard.CardName.Substring(randCard.CardName.Length - 2), out p2CardValTemp);
        
        //check if the card value is equal to p1's card and pick again
        while (_p1.Card.CardValue == p2CardValTemp)
        {
            randCard = PickRandomCard();
            int.TryParse(randCard.CardName.Substring(randCard.CardName.Length - 2), out p2CardValTemp);
        }
        
        card = Instantiate(randCard.CardGameObject,_p2.PlayerGameObject.position,Quaternion.Euler(-90, 0, 180),_p2.PlayerGameObject);
        card.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        _p2.Card = new Card(randCard.CardName,card);
        int.TryParse(randCard.CardName.Substring(randCard.CardName.Length - 2), out _p2.Card.CardValue);
    }

    private void DestroyCard(PlayerData player)
    {
        if (player.Card.CardGameObject != null)
        {
            Destroy(player.Card.CardGameObject);
            player.Card = new Card();
        }
    }

    private void CardAnimationFinished(PlayerData player)
    {
        if (_p1.Card.AnimationState == CardAnimation.Revealed && _p2.Card.AnimationState == CardAnimation.Revealed)
        {
            if (_p1.Card.CardValue > _p2.Card.CardValue)
            {
                Debug.Log("Player 1 wins!");
                OnScoreChanged?.Invoke(_p1);
            }
            else
            {
                Debug.Log("Player 2 wins!");
                OnScoreChanged?.Invoke(_p2);
            }
        }
    }

    private void ScoreChanged(PlayerData player)
    {
        player.PlayerScoreUI.text = $"P{player.PlayerId}: {IncrementScore(player.PlayerId)}";
        if (_p1.Score == maximumScore || _p2.Score == maximumScore)
        {
            if (IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("ScoreScene", LoadSceneMode.Single);
            }
        }
        else
        {
            StartCoroutine(TransitionNextRound(roundTransitionDuration));
        }
    }

    private Card PickRandomCard()
    {
        int rand = Random.Range(0, _playingCards.Count);
        
        return new Card(_playingCards.ElementAt(rand).Key, _playingCards.ElementAt(rand).Value);
    }


    private void UpdateRotationState(int playerId, CardAnimation state)
    {
        if (playerId == 1) _p1.Card.AnimationState = state;
        if (playerId == 2) _p2.Card.AnimationState = state;
    }
    
    private int IncrementScore(int playerId)
    {
        if (playerId == 1)
        {
            _p1.Score++;
            return _p1.Score;
        }
        _p2.Score++;
        return _p2.Score;
    }
    
    IEnumerator RotateCard(PlayerData p)
    {
        float time = 0;
        Quaternion startRotation = Quaternion.Euler(-90, 0, 180);
        Quaternion endRotation = Quaternion.Euler(-90, 0, 0);
        UpdateRotationState(p.PlayerId, CardAnimation.Revealing);
        while (time < animationDuration)
        {
            p.Card.CardGameObject.transform.rotation = Quaternion.Lerp(startRotation, endRotation, time / animationDuration);
            time += Time.deltaTime;
            yield return null;
        }

        p.Card.CardGameObject.transform.rotation = endRotation; // Ensure the rotation is set to the final value
        UpdateRotationState(p.PlayerId, CardAnimation.Revealed);
        OnCardAnimationFinished?.Invoke(p);
    }

    IEnumerator TransitionNextRound(float duration)
    {
        yield return new WaitForSeconds(duration);
        SpawnCardsClientRpc();
        _playBtnP1.enabled = true;
        _playBtnP2.enabled = true;
    }

    private void Update()
    {
        if (IsServer)
        {
            PlayerCount = NetworkManager.Singleton.ConnectedClients.Count;
            // Rest of your code
            //Debug.Log(PlayerCount.ToString());
        }
    }
}
