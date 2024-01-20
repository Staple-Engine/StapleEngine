using Staple;

public class MyAsset : IStapleAsset, IGuidAsset
{
    //Add fields here

    public string Guid { get; set; }

    public static object Create(string guid)
    {
        return Resources.Load<MyAsset>(guid);
    }
}
