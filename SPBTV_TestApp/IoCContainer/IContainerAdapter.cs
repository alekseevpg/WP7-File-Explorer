namespace SPBTV_TestApp.IoCContainer
{
    public interface IContainerAdapter
    {
        T Resolve<T>() where T : class;
    }
}