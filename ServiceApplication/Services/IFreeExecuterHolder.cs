namespace ServiceApplication.Services
{
    public interface IFreeExecuterHolder
    {
        void Add(IExecuterConnection item);
        IExecuterConnection Get();
    }
}
