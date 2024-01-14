using Firebase.Storage;
using Firebase.Extensions;
using UnityEngine;
using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Database;
using System.IO;



public class DLC_controller : MonoBehaviour
{
    private FirebaseStorage _instance;
    private static DatabaseReference _rtdReference;
    [SerializeField] private GameObject SaleItemPrefab;
    [SerializeField] private GameObject SaleItemGroup;
    int Currency;
    private TextMeshProUGUI currencyText;
    public GameObject MenuBtn;
    public GameObject effect;
    GameManager gameManager;
    //private RawImage _image;
    public Slider progressBar;
    [SerializeField] private string _playerId;

    // Start is called before the first frame update
    void Start()
    {
        _instance = FirebaseStorage.DefaultInstance;
        _rtdReference = FirebaseDatabase.DefaultInstance.RootReference;
        DownloadFile(_instance.GetReferenceFromUrl("gs://homeassignment-ea4dc.appspot.com/Manifest.xml"));
        Currency = PlayerPrefs.GetInt("myCurrency");
        currencyText = GameObject.Find("Currency").GetComponent<TextMeshProUGUI>();
        Debug.Log("My Currency is: " + Currency.ToString());
        MenuBtn.GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadSceneAsync("WelcomeScene"));
        //DONT FORGET TO PRESS RESET DATA BUTTON WHEN ENTERING PLAYMODE
        string _savedPlayerId = PlayerPrefs.GetString("myKey");

        if (!String.IsNullOrEmpty(_savedPlayerId))
        {
            _playerId = _savedPlayerId;
        }
        else
        {
            _playerId = GenerateUserId();
            PlayerPrefs.SetString("PlayerID", _playerId);
        }

    }

    public static string GenerateUserId()
    {
        return Guid.NewGuid().ToString();
    }

    public void SaveNewAction(string userAction)
    {
        PurchaseData data = new PurchaseData(_playerId, userAction);
        string json = JsonUtility.ToJson(data);
        _rtdReference.Child("Purchases").Child(_playerId).Child(data.Timestamp).SetRawJsonValueAsync(json);
        //Dictionary<string, System.Object> playerDict = data.ToDictionary();
    }

    public void UpdateProgressBar(float progress)
    {
        progressBar.value = progress;
    }

    public void DownloadFile(StorageReference reference)
    {

        // Create local filesystem URL
        string localfile = Application.persistentDataPath + "/Manifest.xml";
        Debug.Log("Downloading to: " + localfile);

        // Start downloading a file
        Task task = reference.GetFileAsync(localfile,
            new StorageProgress<DownloadState>(state =>
            {
                // called periodically during the download
                Debug.Log(String.Format(
                    "Progress: {0} of {1} bytes transferred.",
                    state.BytesTransferred,
                    state.TotalByteCount
                ));
            }), CancellationToken.None);

        task.ContinueWithOnMainThread(resultTask =>
        {
            if (!resultTask.IsFaulted && !resultTask.IsCanceled)
            {
                Debug.Log("Download finished.");
                ReadManifest(Application.persistentDataPath + "/Manifest.xml");
            }
        });
    }

    public void DownloadImage(StorageReference reference, RawImage rawImage)
    {
        // Download in memory with a maximum allowed size of 2MB (2 * 1024 * 1024 bytes)
        const long maxAllowedSize = 6 * 1024 * 1024;
        reference.GetBytesAsync(maxAllowedSize).ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogException(task.Exception);
                // Uh-oh, an error occurred!
            }
            else
            {
                byte[] fileContents = task.Result;
                Debug.Log("Finished downloading!");
                Texture2D tex = new Texture2D(483, 617);
                tex.LoadImage(fileContents);
                rawImage.texture = tex;
                rawImage.transform.parent.gameObject.SetActive(true);
            }
        });



    }

    public void DownloadMainImage(StorageReference reference, string FileName)
    {
        // Download in memory with a maximum allowed size of 6MB (6 * 1024 * 1024 bytes)
        const long maxAllowedSize = 6 * 1024 * 1024;

        // Start downloading a file
        Task<byte[]> task = reference.GetBytesAsync(maxAllowedSize,
            new StorageProgress<DownloadState>(state =>
            {
                progressBar.value = state.BytesTransferred;
                progressBar.maxValue = state.TotalByteCount;
            }), CancellationToken.None);

        task.ContinueWithOnMainThread(resultTask =>
        {
            if (resultTask.IsFaulted || resultTask.IsCanceled)
            {
                Debug.LogException(resultTask.Exception);
                // Uh-oh, an error occurred!
            }
            else
            {
                byte[] fileContents = resultTask.Result;

                // Create the directory if it doesn't exist
                string directoryPath = Path.Combine(Application.persistentDataPath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Combine the directory and file name for the complete path
                string filePath = Path.Combine(directoryPath, $"{FileName}.jpg");

                // Check if the file already exists and handle it accordingly
                if (File.Exists(filePath))
                {
                    Debug.LogWarning($"File {FileName}.jpg already exists. Skipping download.");
                    GameObject go = Instantiate(effect);
                    Destroy(go, 5); // Destroy after 5 seconds
                }
                else
                {
                    // Write the file contents to the specified path
                    System.IO.File.WriteAllBytes(filePath, fileContents);
                    Debug.Log($"Finished downloading Main Image. Saved at: {filePath}");

                    // Instantiate an effect
                    GameObject go = Instantiate(effect);
                    Destroy(go, 5); // Destroy after 5 seconds
                }
            }
        });
    }



    public void ReadManifest(string path)
    {
        Repository repository = RepositoryReader.ReadRepository(path);
        foreach (Asset a in repository.Assets)
        {
            GameObject saleItem = Instantiate(SaleItemPrefab, new Vector3(0, 0, 0), Quaternion.identity, SaleItemGroup.transform);
            saleItem.transform.localPosition = Vector3.zero;
            saleItem.name = a.Id.ToString();
            saleItem.SetActive(false);
            RawImage rawImg = saleItem.transform.GetChild(2).GetComponent<RawImage>();

            //getting description from menifest
            saleItem.transform.GetChild(0).GetComponent<TMPro.TMP_Text>().text = a.Description;

            //Getting price for europe from manifest
            saleItem.transform.GetChild(1).GetComponent<TMPro.TMP_Text>().text = $"{a.Price.ToString()}";

            saleItem.transform.GetChild(5).gameObject.SetActive(false);

            Debug.Log(repository.BaseUrl + '/' + a.Image);
            DownloadImage(_instance.GetReferenceFromUrl(repository.BaseUrl + '/' + a.Image + ".png"), rawImg);

            int DeductBalance()
            {   if (Currency < a.Price)
                {
                    Debug.Log("Not enough Money");
                    //PlayerPrefs.SetInt("myCurrency", Currency);
                    return Currency;
                }
                else if (Currency > a.Price)
                {
                    int newBalance = Currency -= a.Price;
                    Debug.Log("My New Balance is: " + Currency.ToString());
                    if (a.Name == "Background 1")
                    {
                        PlayerPrefs.SetInt("PurchasedBK1", 1);
                        SaveNewAction("PURCHASE_BK1");
                        saleItem.transform.GetChild(5).gameObject.SetActive(true);
                        saleItem.transform.GetChild(4).gameObject.SetActive(false);
                        Debug.Log("Player baught" + a.Name);
                        Debug.Log("Downloading" + a.Name);
                        DownloadMainImage(_instance.GetReferenceFromUrl(repository.BaseUrl + '/' + a.Main_Image + ".jpg"), a.Main_Image);
                    }
                    if (a.Name == "Background 2")
                    {
                        PlayerPrefs.SetInt("PurchasedBK2", 1);
                        SaveNewAction("PURCHASE_BK2");
                        saleItem.transform.GetChild(5).gameObject.SetActive(true);
                        saleItem.transform.GetChild(4).gameObject.SetActive(false);
                        Debug.Log("Player baught" + a.Name);
                        DownloadMainImage(_instance.GetReferenceFromUrl(repository.BaseUrl + '/' + a.Main_Image + ".jpg"), a.Main_Image);
                    }
                    if (a.Name == "Background 3")
                    {
                        PlayerPrefs.SetInt("PurchasedBK3", 1);
                        SaveNewAction("PURCHASE_BK3");
                        saleItem.transform.GetChild(5).gameObject.SetActive(true);
                        saleItem.transform.GetChild(4).gameObject.SetActive(false);
                        Debug.Log("Player baught" + a.Name);
                        DownloadMainImage(_instance.GetReferenceFromUrl(repository.BaseUrl + '/' + a.Main_Image + ".jpg"), a.Main_Image);
                    }
                    PlayerPrefs.SetInt("myCurrency", Currency);
                    return newBalance;
                }
                return Currency;
            }

            

            if (PlayerPrefs.GetInt("PurchasedBK1") == 1) 
            {
                //right now its setting active all the buttons i need to change this code to make only the correpsonding item active
                if (a.Name == "Background 1")
                {
                    Debug.Log("I bought pic 1 already");
                    saleItem.transform.GetChild(5).gameObject.SetActive(true);
                    saleItem.transform.GetChild(4).gameObject.SetActive(false);
                    
                }
                
            }
            if (PlayerPrefs.GetInt("PurchasedBK2") == 1)
            {
                //right now its setting active all the buttons i need to change this code to make only the correpsonding item active
                if (a.Name == "Background 2")
                {
                    Debug.Log("I bought pic 2 already");
                    saleItem.transform.GetChild(5).gameObject.SetActive(true);
                    saleItem.transform.GetChild(4).gameObject.SetActive(false);
                }

            }
            if (PlayerPrefs.GetInt("PurchasedBK3") == 1)
            {
                //right now its setting active all the buttons i need to change this code to make only the correpsonding item active
                if (a.Name == "Background 3")
                {
                    Debug.Log("I bought pic 2 already");
                    saleItem.transform.GetChild(5).gameObject.SetActive(true);
                    saleItem.transform.GetChild(4).gameObject.SetActive(false);
                }

            }

            void ActiveBK_btn()
            {
                //in this method change the player prefs of the active pic;
                if (a.Name == "Background 1")
                {
                    Debug.Log("Active bk1");
                    PlayerPrefs.SetInt("ActiveBK1", 1);
                    PlayerPrefs.SetInt("ActiveBK2", 0);
                    PlayerPrefs.SetInt("ActiveBK3", 0);
                }
                if (a.Name == "Background 2")
                {
                    Debug.Log("Active bk2");
                    PlayerPrefs.SetInt("ActiveBK2", 1);
                    PlayerPrefs.SetInt("ActiveBK1", 0);
                    PlayerPrefs.SetInt("ActiveBK3", 0);
                }
                if (a.Name == "Background 3")
                {
                    Debug.Log("Active bk3");
                    PlayerPrefs.SetInt("ActiveBK3", 1);
                    PlayerPrefs.SetInt("ActiveBK2", 0);
                    PlayerPrefs.SetInt("ActiveBK1", 0);
                }
            }

            saleItem.transform.GetChild(4).GetComponent<Button>().onClick.AddListener(() => DeductBalance());
            saleItem.transform.GetChild(5).GetComponent<Button>().onClick.AddListener(() => ActiveBK_btn());
        }
    }

    // Update is called once per frame
    void Update()
    {
        currencyText.text = Currency.ToString();
    }

}
