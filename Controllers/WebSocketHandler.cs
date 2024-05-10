//using Microsoft.AspNetCore.Http;
//using Newtonsoft.Json;
//using OpenAICustomFunctionCallingAPI.API.DTOs;
//using OpenAICustomFunctionCallingAPI.Business;
//using OpenAICustomFunctionCallingAPI.Controllers.DTOs;
//using OpenAICustomFunctionCallingAPI.Host.Config;
//using System;
//using System.Net.WebSockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace OpenAICustomFunctionCallingAPI.Controllers.WebSocketHandler
//{
//    public class WebSocketHandler : IWebSocketHandler
//    {
//        private readonly ICompletionLogic _completionLogic;
//        private readonly IValidationLogic _validationLogic;

//        public WebSocketHandler(Settings settings)
//        {
//            _completionLogic = new CompletionLogic(settings);
//            _validationLogic = new ValidationLogic();
//        }

//        public async Task InvokeAsync(HttpContext context, Func<Task> next)
//        {
//            if (context.WebSockets.IsWebSocketRequest)
//            {
//                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
//                await HandleWebSocketAsync(webSocket);
//            }
//            else
//            {
//                await next();
//            }
//        }

//        private async Task HandleWebSocketAsync(WebSocket webSocket)
//        {
//            var buffer = new byte[1024 * 4];

//            while (webSocket.State == WebSocketState.Open)
//            {
//                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
//                if (result.MessageType == WebSocketMessageType.Text)
//                {
//                    var payload = Encoding.UTF8.GetString(buffer, 0, result.Count);

//                    // Deserialize the incoming JSON payload into ChatRequestDTO format
//                    var chatRequest = JsonConvert.DeserializeObject<ChatRequestDTO>(payload);

//                    var errorMessage = _validationLogic.ValidateChatRequest(null, chatRequest);
//                    if (errorMessage != null) 
//                    {
//                        await SendMessage(webSocket, $"Validation Error: {errorMessage}");
//                    }
//                    else
//                    {
//                        // Process the chat request and send the result back
//                        var response = await _completionLogic.ProcessCompletionRequest(chatRequest);
//                        await SendMessage(webSocket, JsonConvert.SerializeObject(response));
//                    }
//                }
//                else if (result.MessageType == WebSocketMessageType.Close)
//                {
//                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
//                }
//            }
//        }

//        private async Task SendMessage(WebSocket webSocket, string message)
//        {
//            var buffer = Encoding.UTF8.GetBytes(message);
//            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
//        }
//    }
//}