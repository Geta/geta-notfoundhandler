namespace Geta.NotFoundHandler.Data;

public interface IRegexRedirectOrderUpdater
{
    public void UpdateOrder(bool isIncrease = false);
}
