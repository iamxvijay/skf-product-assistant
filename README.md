
# SKF Product Assistant Mini

The **SKF Product Assistant Mini** is an AI-powered tool for exploring SKF bearing data through natural, conversational interactions. It's designed to go beyond simple attribute lookups—users can ask follow-up questions, compare products, and reference prior context just like speaking with a real assistant.

✅ Fully functional | ⚙️ Scalable design | 🔒 Secure architecture | 🚀 Free to test

---

## 🌐 Live Demo

🧪 Try it here:  
**[SKF Assistant Web App](https://skf-assistant-web-a6fdardwh2dkemag.southindia-01.azurewebsites.net)**  
📦 Source Code:  
**[GitHub Repository](https://github.com/iamxvijay/skf-product-assistant)**

---

## 💡 Key Features

- **Conversational Intelligence**  
  Ask follow-up questions, reference previously discussed products, and make comparisons effortlessly.

- **Shared Memory Across Assistants**  
  Two synchronized AI agents collaborate with shared context to deliver accurate and fluid responses.

- **Smart Caching with Redis**  
  Fast response times and reduced compute costs thanks to intelligent caching of large files and repeated queries.

- **Secure API Handling**  
  Backend is fully isolated from frontend. API keys are stored in environment variables, with plans to migrate to **Azure Key Vault**.

- **SSO/OAuth Ready**  
  Supports future integration with **OAuth / SSO** for organization-level access control.

- **Scalable Design**  
  Currently demoing two products—designed to expand seamlessly to support many more.

---

## 🧪 Sample Queries to Try

You can start by asking questions like:

- `What is the outside diameter of 6205?`
- `Tell me the width for the 6205 N bearing.`
- `What material is used in 6205 N?`
- `Does the 6205 bearing have a snap ring groove?`
- `Compare the bore diameter of 6205 and 6205 N.`
- `Which one is heavier, 6205 or 6205 N?`
- `What’s the limiting speed of the 6205 bearing?`
- `Show me the reference speed for 6205 N.`
- `Is there any difference in pack gross weight between 6205 and 6205 N?`
- `If I want a bearing with a “Sheet metal” cage, which one should I pick?`

---

## 🔐 Tech Stack & Architecture

- **Frontend**: React + Tailwind CSS  
- **Backend**: .Net Core 8 (API Layer)  
- **AI Integration**: Azure OpenAI (Chat Completion API)  
- **Memory Management**: Redis (Context & Cache)  
- **Deployment**: Azure Web Apps  
- **Security**: Env-based secrets (Azure Key Vault planned), Backend-Frontend isolation  


