using System.Threading.Tasks;

namespace ServiceApplication.Services
{
    public interface ICloudExecuterFactory
    {
        ValueTask<IExecuterConnection> GetNewAsync();
    }

    public static class CloudExecuterFactoryExtensions
    {
        public static IExecuterConnection GetNew(this ICloudExecuterFactory factory)
        {
            return factory.GetNewAsync().Result;
        }
    }
}
