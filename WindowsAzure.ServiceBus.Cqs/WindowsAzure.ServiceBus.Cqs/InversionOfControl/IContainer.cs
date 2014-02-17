namespace WindowsAzure.ServiceBus.Cqs.InversionOfControl
{
    /// <summary>
    /// Inversion of control container-
    /// </summary>
    public interface IContainer
    {
        /// <summary>
        /// Create a new scoped container
        /// </summary>
        /// <returns>Scoped container.</returns>
        IChildContainer CreateChildContainer();
    }
}