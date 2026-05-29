namespace WEBDEV_Project.Models
{
    public enum ItemType
    {
        Lost,
        Found
    }

    public enum ItemStatus
    {
        Active,
        Resolved,
        Removed,
        Expired
    }

    public enum ClaimStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
