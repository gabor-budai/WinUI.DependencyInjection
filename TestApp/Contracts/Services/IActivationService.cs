namespace TestApp.Contracts.Services;

public interface IActivationService
{
    Task ActivateAsync(object activationArgs);
}
