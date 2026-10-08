namespace Domain.Blacklist;

public interface IBlacklist
{
    bool Contains(string ssn);
}
