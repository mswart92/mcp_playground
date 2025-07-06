namespace PetShop.API.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendOrderConfirmationEmailAsync(string to, string customerName, string orderNumber, decimal totalAmount, List<string> orderItems);
        Task<bool> SendEmailAsync(string to, string subject, string body);
    }
}
