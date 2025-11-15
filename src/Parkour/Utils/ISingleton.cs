namespace Parkour;

interface ISingleton<TSelf>
{
    static abstract TSelf Instance { get; }
}
