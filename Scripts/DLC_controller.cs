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

            Debug.Log(repository.BaseUrl + '/' + a.Image);
            DownloadImage(_instance.GetReferenceFromUrl(repository.BaseUrl + '/' + a.Image + ".jpg"), rawImg);

            int DeductCurrency()
            {   if (Currency < a.Price)
                {
                    Debug.Log("Not enough Money");
                    PlayerPrefs.SetInt("myCurrency", Currency);
                    return Currency;
                }
                else if (Currency > a.Price)
                {
                    int newBalance = Currency -= a.Price;
                    Debug.Log("My New Balance is: " + Currency.ToString());
                    //make new button and call it "set as active background" FOR EXAMPLE THEN MAKE THIS BUTTON TO SETACTIVE(TRUE) AND
                    //AND WHEN THIS BUTTON IS CLICKED MAKE THIS BACKGROUNd THE BACKGROUND IMAGE OF MAIN GAME
                    PlayerPrefs.SetInt("myCurrency", Currency);
                    return newBalance;
                }
                return Currency;
            }

            saleItem.transform.GetChild(4).GetComponent<Button>().onClick.AddListener(() => DeductCurrency());
        }
    }

    // Update is called once per frame
    void Update()
    {
        currencyText.text = Currency.ToString();
    }

}
