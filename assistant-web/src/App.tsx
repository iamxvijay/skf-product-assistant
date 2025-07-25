import React, { useState, useRef, useEffect } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import "./App.css";

type ChatMessage = {
  role: "user" | "assistant";
  content: string;
};

type ApiRequest = {
  sessionId: string;
  userMessage: string;
};

type ApiResponse = {
  sessionId: string;
  response: string;
};

const API_URL = "http://localhost:5275/api/Chat/ask";

function App() {
  const [chat, setChat] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const [sessionId, setSessionId] = useState<string>("");
  const [loading, setLoading] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // Always scroll to bottom when chat changes
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [chat]);

  useEffect(() => {
    if (!loading) {
      textareaRef.current?.focus();
    }
  }, [loading]);

  const handleInputKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleSend = async () => {
    if (!input.trim()) return;
    setLoading(true);
    const newChat = [...chat, { role: "user", content: input }];
    setChat(newChat);
    try {
      const body: ApiRequest = {
        sessionId: sessionId || "",
        userMessage: input
      };
      const resp = await fetch(API_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
      });
      const data: ApiResponse = await resp.json();
      setSessionId(data.sessionId);
      setChat((c) => [...c, { role: "assistant", content: data.response }]);
      setInput("");
    } catch (e) {
      setChat((c) => [...c, { role: "assistant", content: "Error contacting server." }]);
    }
    setLoading(false);
    
  };

  return (
    <div className="container">
      <header className="header">SKF Product Assistant</header>
      <main className="chat-main">
        <div className="chat-history">
          {chat.length === 0 && (
            <div className="placeholder">Start the conversation…</div>
          )}
          {chat.map((msg, i) => (
            <div
              key={i}
              className={`chat-msg ${msg.role === "user" ? "user" : "assistant"}`}
            >
              {msg.role === "assistant" ? (
                <div className="bubble">
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>{msg.content}</ReactMarkdown>
                </div>
              ) : (
                <div className="bubble">{msg.content}</div>
              )}
            </div>
          ))}
          <div ref={messagesEndRef} />
        </div>
      </main>
      <footer className="chat-footer">
        <div className="input-area">
          <textarea
            ref={textareaRef}
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={handleInputKeyDown}
            placeholder="Type your message…"
            rows={2}
            disabled={loading}
          />
          <button onClick={handleSend} disabled={loading || !input.trim()}>
            {loading ? "Sending…" : "Send"}
          </button>
        </div>
        <div className="footer-note">
          <small>Press <b>Enter</b> to send, <b>Shift+Enter</b> for new line</small>
        </div>
      </footer>
    </div>
  );
}

export default App;
