using System;
using System.Collections.Generic;


[Serializable]
public class GameData
{
    private string _id;
    private string _timestamp;
    public string Winner;
    public string Loser;
    public string Duration;

    public string ID => _id;
    public string Timestamp => _timestamp;


    public GameData(string id, string winner, string loser, string duration)
    {
        _id = id;
        _timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Winner = winner;
        Loser = loser;
        Duration = duration + " seconds";
    }
}
