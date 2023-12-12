using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayingCards", menuName = "ScriptableObjects/PlayingCards", order = 1)]
public class PlayingCardsSO : ScriptableObject
{
    [SerializeField] private List<GameObject> playingCardsList;
    [SerializeField] private List<Texture2D> backgrounds;
    public Dictionary<string, GameObject> PlayingCardsDict
    {
        get
        {
            Dictionary<string, GameObject> cardData = new Dictionary<string, GameObject>();
            foreach (GameObject card in playingCardsList)
            {
                cardData.Add(card.name.Split("_")[card.name.Split("_").Length -2],card);
            }
            return cardData;
        }
    }

    public List<Texture2D> Backgrounds
    {
        get
        {
            //If there are backgrounds in the SO, return them
            if (backgrounds != null && backgrounds.Count > 0) return backgrounds;
            //If there arent, add one and return
            Texture2D defaultTex = GenerateGreenTexture(1600, 900, new Color(53, 101, 77));
            return new List<Texture2D> {defaultTex};
        }
        set => backgrounds = value;
    }

    private Texture2D GenerateGreenTexture(int width,int height,Color color)
    {
        Texture2D texture = new Texture2D(width, height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

}
