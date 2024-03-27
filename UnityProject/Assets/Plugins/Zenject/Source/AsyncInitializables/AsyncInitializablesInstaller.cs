namespace Zenject
{
    public class AsyncInitializablesInstaller : Installer<AsyncInitializablesInstaller>
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<AsyncInitializableManager>().AsSingle().CopyIntoAllSubContainers();
        }
    }
}