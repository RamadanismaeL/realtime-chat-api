/*
*@author Ramadan Ismael
*/

namespace server.src.DTOs
{
    public class MessageRequestDto
    {
        public int Id { get; set; }
        public string? SenderId { get; set; }
        public string? ReceiveId { get; set; }
        public string? Content { get; set; }        
        public bool IsRead { get; set; }
        public DateTime CreateDate { get; set; }
    }
}