using System;
using System.Collections.Generic;

public enum PlayerActions
{
    USER_PURCHASE_BK1,
    USER_PURCHASE_BK2,
    USER_PURCHASE_BK3,
}

[Serializable]
public class PurchaseData
{
    private string _id;
    private string _timestamp;
    public string UserAction;

    public string ID => _id;
    public string Timestamp => _timestamp;


    public PurchaseData(string id, string userAction)
    {
        _id = id;
        _timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        UserAction = userAction;
    }
}
