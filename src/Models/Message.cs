/*
*@author Ramadan Ismael
*/

namespace server.src.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string? SenderId { get; set; }
        public string? ReceiveId { get; set; }
        public string? Content { get; set; }
        public DateTime CreateDate { get; set; }
        public bool IsRead { get; set; }
        public AppUser? Sender { get; set; }
        public AppUser? Receiver { get; set; }
    }
}