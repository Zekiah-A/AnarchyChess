namespace AnarchyChess.Server;

/// <summary>
/// A dictionary type where the both the keys and values must be  unique, and can be directly accessed in both directions.
/// </summary>
public class DualDictionary<TKey, TValue> where TKey : notnull where TValue : notnull
{
    private Dictionary<TKey, TValue> Forward { get; set; }
    private Dictionary<TValue, TKey> Reverse { get; set; }
    public Dictionary<TKey, TValue>.KeyCollection Keys => Forward.Keys;
    public Dictionary<TValue, TKey>.KeyCollection Values => Reverse.Keys;

    public int Count => Forward.Count;

    public DualDictionary()
    {
        Forward = new Dictionary<TKey, TValue>();
        Reverse = new Dictionary<TValue, TKey>();
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Forward.Add(item.Key, item.Value);
        Reverse.Add(item.Value, item.Key);
    }

    public void Clear()
    {
        Forward = new Dictionary<TKey, TValue>();
        Reverse = new Dictionary<TValue, TKey>();
    }
    
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return Forward.ContainsKey(item.Key) && Reverse.ContainsKey(item.Value);
    }
    
    public void Add(TKey key, TValue value)
    {
        Forward.Add(key, value);
        Reverse.Add(value, key);
    }

    public bool ContainsKey(TKey key)
    {
        return Forward.ContainsKey(key);
    }

    public bool ContainsValue(TValue value)
    {
        return Reverse.ContainsKey(value);
    }
    
    public bool Remove(TKey key)
    {
        if (!Forward.ContainsKey(key))
        {
            return false;
        }
        
        var value = Forward[key];
        Forward.Remove(key);
        Reverse.Remove(value);
        return true;
    }
    
    public bool Remove(TValue value)
    {
        if (!Reverse.ContainsKey(value))
        {
            return false;
        }

        var key = Reverse[value];
        Reverse.Remove(value);
        Forward.Remove(key);
        return true;
    }

    public TKey GetKey(TValue value) => Reverse[value];

    public TValue GetValue(TKey key) => Forward[key];

    public bool TryGetKey(TValue value, out TKey key)
    {
        if (!Reverse.ContainsKey(value))
        {
            key = default!;
            return false;
        }
        
        key = Reverse[value];
        return true;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (!Forward.ContainsKey(key))
        {
            value = default!;
            return false;
        }
        
        value = Forward[key];
        return true;
    }
}