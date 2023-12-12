using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

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

public class GameManager : MonoBehaviour
{
    [SerializeField] private PlayingCardsSO _playingCardsSO;
    [SerializeField] private int maximumScore = 5;
    [SerializeField] private float animationDuration = 2f;
    [SerializeField] private float roundTransitionDuration = 2f;
    private Dictionary<string, GameObject> _playingCards;
    private List<Texture2D> _backgrounds;
    
    private PlayerData _p1;
    private PlayerData _p2;

    private Button _playBtnP1;
    private Button _playBtnP2;
    
    private delegate void CardAnimationFinishedDelegate(PlayerData player);
    private event CardAnimationFinishedDelegate OnCardAnimationFinished;
    private delegate void ScoreChangedDelegate(PlayerData player);
    private event ScoreChangedDelegate OnScoreChanged;
    
    private static GameManager _instance;
    public static GameManager Singleton
    {
        get  => _instance;
        private set
        {
            if (_instance != null && _instance != value)
            {
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
    }

    private void Start()
    {
        PlayerPrefs.SetInt("myCurrency", 1000);
    }

    private void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "WelcomeScene":
                GameObject.Find("StoreBtn").GetComponent<Button>().onClick.AddListener(()=>SceneManager.LoadSceneAsync("StoreScene"));
                //Change these for multiplayer
                GameObject.Find("HostBtn").GetComponent<Button>().onClick.AddListener(()=>SceneManager.LoadSceneAsync("GameScene"));
                GameObject.Find("JoinBtn").GetComponent<Button>().onClick.AddListener(()=>SceneManager.LoadSceneAsync("GameScene"));
                break;
            case "GameScene":
                _playBtnP1 = GameObject.Find("PlayBtnP1").GetComponent<Button>();
                _playBtnP2 = GameObject.Find("PlayBtnP2").GetComponent<Button>();
                _p1 = new PlayerData(1,GameObject.Find("Player1").transform,GameObject.Find("P1Score").GetComponent<TMP_Text>(), new Card());
                _p2 = new PlayerData(2, GameObject.Find("Player2").transform,GameObject.Find("P2Score").GetComponent<TMP_Text>(), new Card());
                _playBtnP1.onClick.AddListener(() =>
                {
                    _playBtnP1.enabled = false;
                    StartCoroutine(RotateCard(_p1));
                });
                _playBtnP2.onClick.AddListener(()=>
                {
                    _playBtnP2.enabled = false;
                    StartCoroutine(RotateCard(_p2));
                });
                SpawnCards();
                break;
            case "ScoreScene":
                GameObject.Find("BackBtn").GetComponent<Button>().onClick.AddListener(()=>SceneManager.LoadSceneAsync("WelcomeScene"));
                TMP_Text result = GameObject.Find("Winner").GetComponent<TMP_Text>();
                if (_p1.Score > _p2.Score)
                {
                    result.text = "Player 1 Wins!";
                }
                else
                {
                    result.text = "Player 2 Wins!";
                }
                break;
            case "StoreScene":
                break;
        }
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        OnCardAnimationFinished -= CardAnimationFinished;
        OnScoreChanged -= ScoreChanged;
    }

    public void SpawnCards()
    {
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
            SceneManager.LoadSceneAsync("ScoreScene");
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
        SpawnCards();
        _playBtnP1.enabled = true;
        _playBtnP2.enabled = true;
    }
}
