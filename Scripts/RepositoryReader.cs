using System.Xml.Linq;

public static class RepositoryReader
{
    public static Repository ReadRepository(string xmlPath)
    {
        // Load the XML file into an XDocument object.
        XDocument document = XDocument.Load(xmlPath);

        // Get the root element of the XML document.
        XElement repositoryElement = document.Root;

        // Create a new Repository object.
        Repository repository = new Repository();

        repository.BaseUrl = repositoryElement.Element("baseUrl").Value;

        // Populate the Repository object with the data from the XML file.
    
  

        // Iterate over the assets element of the repository element.
        foreach (XElement assetElement in repositoryElement.Element("assets").Elements("asset"))
        {
            // Create a new Asset object and populate it with the data from the XML element.
            Asset asset = new Asset();
            asset.Id = int.Parse(assetElement.Element("id").Value);
            asset.Name = assetElement.Element("name").Value;
            asset.Description = assetElement.Element("desc").Value;
            asset.Image = assetElement.Element("image").Value;
            asset.Main_Image = assetElement.Element("backImg").Value;
            asset.Price = int.Parse(assetElement.Element("price").Value);
            // Add the Asset object to the Repository object.
            repository.Assets.Add(asset);
        }

        // Return the Repository object.
        return repository;
    }
}