using Firebase.Storage;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Xml.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEngine.SceneManagement;

public class DLC_controller : MonoBehaviour
{
    private FirebaseStorage _instance;
    [SerializeField] private GameObject SaleItemPrefab;
    [SerializeField] private GameObject SaleItemGroup;
    int Currency;
    private TextMeshProUGUI currencyText;
    public GameObject MenuBtn;
    GameManager gameManager;
    //private RawImage _image;

    // Start is called before the first frame update
    void Start()
    {
        _instance = FirebaseStorage.DefaultInstance;
        DownloadFile(_instance.GetReferenceFromUrl("gs://homeassignment-ea4dc.appspot.com/Manifest.xml"));
        Currency = PlayerPrefs.GetInt("myCurrency");
        currencyText = GameObject.Find("Currency").GetComponent<TextMeshProUGUI>();
        Debug.Log("My Currency is: " + Currency.ToString());
        MenuBtn.GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadSceneAsync("WelcomeScene"));
        //DONT FORGET TO PRESS RESET DATA BUTTON WHEN ENTERING PLAYMODE
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
            DownloadImage(_instance.GetReferenceFromUrl(repository.BaseUrl + '/' + a.Image + ".jpg"), rawImg);

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
                    if (a.Name == "Pic 1")
                    {
                        PlayerPrefs.SetInt("PurchasedBK1", 1);
                        saleItem.transform.GetChild(5).gameObject.SetActive(true);
                        saleItem.transform.GetChild(4).gameObject.SetActive(false);
                        Debug.Log("Player baught" + a.Name);
                    }
                    if (a.Name == "Pic 2")
                    {
                        PlayerPrefs.SetInt("PurchasedBK2", 1);
                        saleItem.transform.GetChild(5).gameObject.SetActive(true);
                        saleItem.transform.GetChild(4).gameObject.SetActive(false);
                        Debug.Log("Player baught" + a.Name);
                    }
                    if (a.Name == "Pic 3")
                    {
                        PlayerPrefs.SetInt("PurchasedBK3", 1);
                        saleItem.transform.GetChild(5).gameObject.SetActive(true);
                        saleItem.transform.GetChild(4).gameObject.SetActive(false);
                        Debug.Log("Player baught" + a.Name);
                    }
                    PlayerPrefs.SetInt("myCurrency", Currency);
                    return newBalance;
                }
                return Currency;
            }

            void ActiveBK_btn()
            {
                //in this method change the player prefs of the active pic;
            }

            if (PlayerPrefs.GetInt("PurchasedBK1") == 1) 
            {
                //right now its setting active all the buttons i need to change this code to make only the correpsonding item active
                if (a.Name == "Pic 1")
                {
                    Debug.Log("I bought pic 1 already");
                    saleItem.transform.GetChild(5).gameObject.SetActive(true);
                    saleItem.transform.GetChild(4).gameObject.SetActive(false);
                }
                
            }
            if (PlayerPrefs.GetInt("PurchasedBK2") == 1)
            {
                //right now its setting active all the buttons i need to change this code to make only the correpsonding item active
                if (a.Name == "Pic 2")
                {
                    Debug.Log("I bought pic 2 already");
                    saleItem.transform.GetChild(5).gameObject.SetActive(true);
                    saleItem.transform.GetChild(4).gameObject.SetActive(false);
                }

            }
            if (PlayerPrefs.GetInt("PurchasedBK3") == 1)
            {
                //right now its setting active all the buttons i need to change this code to make only the correpsonding item active
                if (a.Name == "Pic 3")
                {
                    Debug.Log("I bought pic 2 already");
                    saleItem.transform.GetChild(5).gameObject.SetActive(true);
                    saleItem.transform.GetChild(4).gameObject.SetActive(false);
                }

            }

            saleItem.transform.GetChild(4).GetComponent<Button>().onClick.AddListener(() => DeductBalance());
            saleItem.transform.GetChild(5).GetComponent<Button>().onClick.AddListener(() => Debug.Log("Acitve background button for " + a.Name));
        }
    }

    // Update is called once per frame
    void Update()
    {
        currencyText.text = Currency.ToString();
    }

}
