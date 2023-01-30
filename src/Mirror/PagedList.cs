namespace Mirror;

public abstract class PagedList<T>
{
    public Links? Links { get; set; }
    public abstract IEnumerable<T> GetItems();
}
