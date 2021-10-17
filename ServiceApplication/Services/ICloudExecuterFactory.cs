using System.Threading.Tasks;

namespace ServiceApplication.Services
{
    public interface ICloudExecuterFactory
    {
        ValueTask<IExecuterConnection> GetNewAsync();
    }
}
