using System;
using System.Collections.Generic;


[Serializable]
public class GameData
{
    private string _id;
    private string _timestamp;
    private string _winner;
    private string _loser;
    public string _duration;

    public string ID => _id;
    public string Timestamp => _timestamp;

    public string Duration => _duration;

    public string Winner => _winner;

    public string Loser => _loser;


    public GameData(string id, string winner, string loser, string duration)
    {
        _id = id;
        _timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _winner = winner;
        _loser = loser;
        _duration = duration + " seconds";
    }
}
