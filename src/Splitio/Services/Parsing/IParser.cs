using Splitio.Domain;

namespace Splitio.Services.Parsing.Interfaces
{
    public interface IParser<T,P> where T : class where P : class
    {
        P Parse(T entity);
    }
}
