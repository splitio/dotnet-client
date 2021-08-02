namespace Splitio.Services.Common
{
    public interface IBackOff
    {
        double GetInterval(bool inMiliseconds = false);
        void Reset();
        int GetAttempt();
    }
}
