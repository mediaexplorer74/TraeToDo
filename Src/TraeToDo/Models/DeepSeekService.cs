using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TraeToDo.Models
{
    /// <summary>
    /// Service for communicating with the DeepSeek API via OpenRouter
    /// </summary>
    public class DeepSeekService
    {
        // OpenRouter API endpoint compatible with DeepSeek API
        private const string API_URL = "https://openrouter.ai/api/v1/chat/completions";

        // The model to use
        private const string MODEL_NAME = "deepseek/deepseek-r1:free";
        
        // HttpClient for API requests
        private readonly HttpClient _httpClient;
        
        /// <summary>
        /// Creates a new instance of the DeepSeekService
        /// </summary>
        public DeepSeekService()
        {
            _httpClient = new HttpClient();
        }
        
        /// <summary>
        /// Sends a message to the DeepSeek API and gets a response
        /// </summary>
        /// <param name="userMessage">The user's message</param>
        /// <param name="conversationHistory">Previous messages in the conversation</param>
        /// <returns>The AI's response</returns>
        public async Task<string> SendMessageAsync(string userMessage, List<ChatMessage> conversationHistory)
        {
            try
            {
                // Get API key from settings
                string apiKey = SettingsManager.Instance.GetApiKey();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return "Please set your API key in the settings.";
                }
                
                // Set up the request headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://traetodo.app");
                
                // Convert conversation history to the format expected by the API
                var messages = new List<object>();
                
                // Add conversation history
                foreach (var message in conversationHistory)
                {
                    messages.Add(new
                    {
                        role = message.IsUserMessage ? "user" : "assistant",
                        content = message.Content
                    });
                }
                
                // Add the current message
                messages.Add(new
                {
                    role = "user",
                    content = userMessage
                });
                
                // Create the request payload
                var requestData = new
                {
                    model = MODEL_NAME,
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 1000
                };
                
                // Serialize the request to JSON
                string jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                
                // Send the request
                var response = await _httpClient.PostAsync(API_URL, content);
                
                // Process the response
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                    
                    // Extract the assistant's message
                    return responseData.choices[0].message.content.ToString();
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }
    }
}