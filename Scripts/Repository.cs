using System.Collections.Generic;
using System.Globalization;

public class Repository
{
    public string BaseUrl { get; set; }
    
    public List<Asset> Assets { get; set; }

    public Repository()
    {
        Assets = new List<Asset>();
    }
}



public class Asset
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Image { get; set; }
    public string Main_Image { get; set; }
    public int Price { get; set; }

}
