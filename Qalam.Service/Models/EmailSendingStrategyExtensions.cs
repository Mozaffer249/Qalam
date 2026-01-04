namespace Qalam.Service.Models
{
    public static class EmailSendingStrategyExtensions
    {
        public static int ToIntValue(this EmailSendingStrategy strategy)
        {
            return (int)strategy;
        }
    }
}

