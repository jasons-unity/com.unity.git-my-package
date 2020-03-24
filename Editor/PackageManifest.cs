using System;

[Serializable]
public class PackageManifest
{
    public string name;
    public Repository repository;
}

[Serializable]
public class Repository
{
    public string url;
    public string revision;

}