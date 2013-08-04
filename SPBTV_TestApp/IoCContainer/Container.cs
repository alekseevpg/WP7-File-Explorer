namespace SPBTV_TestApp.IoCContainer
{
    public static class Container
    {
        private static IContainerAdapter _containerAdapter;

        public static void Init(IContainerAdapter containerAdapter)
        {
            _containerAdapter = containerAdapter;
        }

        public static T Resolve<T>() where T : class
        {
            return _containerAdapter.Resolve<T>();
        }
    }
}