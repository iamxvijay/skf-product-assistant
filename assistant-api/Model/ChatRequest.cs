namespace assistant_api.Models
{
    public class ChatRequest
    {
        public string SessionId { get; set; }   // Client sends this if continuing a session, or empty for new session
        public string UserMessage { get; set; }
    }
}
